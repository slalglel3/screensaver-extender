using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreensaverExtender
{
    public class TrayApplicationContext : ApplicationContext
    {
        private const string DeveloperId = "slalglel"; // About 메뉴에 표시될 사용자 ID
        private const string AppVersion = "v1.0.3";
        private const int BaseMenuWidth = 250; // 최대 400분대 텍스트("예상 잠금 : 409분 후") 대비 여유있는 고정 폭

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ExtenderEngine _engine;
        private MainForm? _mainForm;

        // Flag to prevent context menu from auto-closing when clicking adjust buttons
        private bool _preventCloseOnItemClick = false;

        // Menu Items for dynamic updates
        private ToolStripMenuItem _itemSummary = null!;
        private ToolStripMenuItem _itemIntervalUp = null!;
        private ToolStripMenuItem _itemIntervalText = null!;
        private ToolStripMenuItem _itemIntervalDown = null!;
        private ToolStripMenuItem _itemCountUp = null!;
        private ToolStripMenuItem _itemCountText = null!;
        private ToolStripMenuItem _itemCountDown = null!;
        private ToolStripMenuItem _itemToggleEnabled = null!;

        public TrayApplicationContext()
        {
            _engine = new ExtenderEngine();
            _engine.SettingsChanged += Engine_SettingsChanged;
            _engine.StateChanged += Engine_StateChanged;

            _contextMenu = CreateContextMenu();

            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateAppIcon(),
                ContextMenuStrip = _contextMenu,
                Text = "스마트 화면보호기 지연기",
                Visible = true
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            UpdateMenuStates();
        }

        private int GetScaledMenuWidth(ContextMenuStrip menu)
        {
            float factor = 1.0f;
            try
            {
                if (menu.DeviceDpi > 0)
                {
                    factor = menu.DeviceDpi / 96.0f;
                }
                else
                {
                    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        factor = g.DpiX / 96.0f;
                    }
                }
            }
            catch
            {
                factor = 1.0f;
            }

            return (int)Math.Round(BaseMenuWidth * factor);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Malgun Gothic", 9.0f);

            // 메뉴 폭 고정: 시간 변경(예: 10분 -> 400분) 시 메뉴 폭이 바뀌거나 흔들리지 않도록 여유있는 고정 폭 적용
            int initialWidth = GetScaledMenuWidth(menu);
            menu.MinimumSize = new Size(initialWidth, 0);
            menu.MaximumSize = new Size(initialWidth, 0);

            menu.Opening += (sender, e) =>
            {
                int targetWidth = GetScaledMenuWidth(menu);
                menu.MinimumSize = new Size(targetWidth, 0);
                menu.MaximumSize = new Size(targetWidth, 0);
            };

            // Keep menu open when clicking arrow buttons or toggle checkbox!
            menu.Closing += (sender, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked && _preventCloseOnItemClick)
                {
                    e.Cancel = true;
                    _preventCloseOnItemClick = false;
                }
            };

            // 1. Summary (예상 잠금 시간)
            _itemSummary = new ToolStripMenuItem("예상 잠금 : 계산 중")
            {
                Enabled = false,
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold)
            };
            menu.Items.Add(_itemSummary);
            menu.Items.Add(new ToolStripSeparator());

            // 2. Interval Group (간결한 텍스트 & 클릭 시 메뉴 유지)
            _itemIntervalUp = new ToolStripMenuItem("▲  +1분", null, (s, e) =>
            {
                _preventCloseOnItemClick = true;
                _engine.IncreaseInterval();
            });
            _itemIntervalText = new ToolStripMenuItem("3분") { Enabled = false };
            _itemIntervalDown = new ToolStripMenuItem("▼  -1분", null, (s, e) =>
            {
                _preventCloseOnItemClick = true;
                _engine.DecreaseInterval();
            });

            menu.Items.Add(_itemIntervalUp);
            menu.Items.Add(_itemIntervalText);
            menu.Items.Add(_itemIntervalDown);
            menu.Items.Add(new ToolStripSeparator());

            // 3. Count Group (간결한 텍스트 & 클릭 시 메뉴 유지)
            _itemCountUp = new ToolStripMenuItem("▲  +1회", null, (s, e) =>
            {
                _preventCloseOnItemClick = true;
                _engine.IncreaseCount();
            });
            _itemCountText = new ToolStripMenuItem("2회") { Enabled = false };
            _itemCountDown = new ToolStripMenuItem("▼  -1회", null, (s, e) =>
            {
                _preventCloseOnItemClick = true;
                _engine.DecreaseCount();
            });

            menu.Items.Add(_itemCountUp);
            menu.Items.Add(_itemCountText);
            menu.Items.Add(_itemCountDown);
            menu.Items.Add(new ToolStripSeparator());

            // 4. Toggle Active (클릭 시 메뉴 유지)
            _itemToggleEnabled = new ToolStripMenuItem("✔ 기능 활성화", null, (s, e) =>
            {
                _preventCloseOnItemClick = true;
                _engine.ToggleEnabled();
            });
            menu.Items.Add(_itemToggleEnabled);

            // 5. Open Settings Dialog
            ToolStripMenuItem itemOpen = new ToolStripMenuItem("세부 설정 창...", null, (s, e) => ShowMainForm());
            menu.Items.Add(itemOpen);
            menu.Items.Add(new ToolStripSeparator());

            // 6. About (마우스 오버 시 우측 세부 메뉴에 아이디 표시)
            ToolStripMenuItem itemAbout = new ToolStripMenuItem("About");
            ToolStripMenuItem itemDevId = new ToolStripMenuItem($"Developer: {DeveloperId}") { Enabled = false };
            ToolStripMenuItem itemVer = new ToolStripMenuItem($"Version: {AppVersion}") { Enabled = false };
            itemAbout.DropDownItems.Add(itemDevId);
            itemAbout.DropDownItems.Add(itemVer);
            menu.Items.Add(itemAbout);

            // 7. Exit
            ToolStripMenuItem itemExit = new ToolStripMenuItem("프로그램 종료", null, (s, e) => ExitProgram());
            menu.Items.Add(itemExit);

            return menu;
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowMainForm();
        }

        private void ShowMainForm()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm(_engine);
            }

            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
            _mainForm.Activate();
        }

        private void Engine_SettingsChanged(object? sender, EventArgs e)
        {
            UpdateMenuStates();
        }

        private void Engine_StateChanged(object? sender, EventArgs e)
        {
            UpdateMenuStates();
        }

        private void UpdateMenuStates()
        {
            _itemSummary.Text = $"예상 잠금 : {_engine.TotalExpectedMinutes}분 후";

            _itemIntervalText.Text = $"{_engine.IntervalMinutes}분 (최대 {_engine.MaxInterval}분)";
            _itemCountText.Text = $"{_engine.MaxSignalCount}회 (최대 {ExtenderEngine.MaxCount}회)";

            // Guard Conditions for Context Menu Arrows
            _itemIntervalUp.Enabled = _engine.CanIncreaseInterval;
            _itemIntervalDown.Enabled = _engine.CanDecreaseInterval;
            _itemCountUp.Enabled = _engine.CanIncreaseCount;
            _itemCountDown.Enabled = _engine.CanDecreaseCount;

            _itemToggleEnabled.Text = _engine.IsEnabled ? "✔ 기능 활성화" : "✖ 비활성화됨";
            _itemToggleEnabled.Checked = _engine.IsEnabled;

            // Update Tray Tooltip
            _notifyIcon.Text = $"화면보호기 지연기 ({_engine.TotalExpectedMinutes}분 후 잠금)";
        }

        private void ExitProgram()
        {
            _notifyIcon.Visible = false;
            _engine.Dispose();
            _mainForm?.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _contextMenu.Dispose();
                _engine.Dispose();
                _mainForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
