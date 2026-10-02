using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreensaverExtender
{
    public class MainForm : Form
    {
        private readonly ExtenderEngine _engine;

        private Label _lblHeader = null!;
        private Label _lblInterval = null!;
        private Button _btnIntervalDown = null!;
        private Button _btnIntervalUp = null!;

        private Label _lblCount = null!;
        private Button _btnCountDown = null!;
        private Button _btnCountUp = null!;

        private CheckBox _chkEnabled = null!;
        private Button _btnClose = null!;

        public MainForm(ExtenderEngine engine)
        {
            _engine = engine;
            InitializeComponent();

            _engine.SettingsChanged += Engine_SettingsChanged;
            _engine.StateChanged += Engine_StateChanged;

            UpdateUiState();
        }

        private void InitializeComponent()
        {
            // 1. Frameless Modern Card Window
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(290, 160);
            this.BackColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular);
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            // Allow dragging the entire window smoothly
            this.MouseDown += EnableWindowDrag;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Hide(); };

            // Line 1: Header (파란 배경의 '예상 잠금 : 10분 후' 표시)
            Panel headerPanel = new Panel
            {
                Location = new Point(1, 1),
                Size = new Size(288, 38),
                BackColor = Color.FromArgb(30, 136, 229) // Material Blue
            };
            headerPanel.MouseDown += EnableWindowDrag;

            _lblHeader = new Label
            {
                Text = "예상 잠금 : 계산 중",
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblHeader.MouseDown += EnableWindowDrag;
            headerPanel.Controls.Add(_lblHeader);
            this.Controls.Add(headerPanel);

            // Line 2: '주기'
            Label lblIntervalTitle = new Label
            {
                Text = "주기",
                Location = new Point(16, 52),
                Size = new Size(38, 20),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };
            this.Controls.Add(lblIntervalTitle);

            _btnIntervalDown = new Button
            {
                Text = "◀",
                Location = new Point(60, 48),
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.System
            };
            _btnIntervalDown.Click += (s, e) => _engine.DecreaseInterval();
            this.Controls.Add(_btnIntervalDown);

            _lblInterval = new Label
            {
                Text = "4분 (최대 4분)",
                Location = new Point(96, 52),
                Size = new Size(125, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(21, 101, 192)
            };
            this.Controls.Add(_lblInterval);

            _btnIntervalUp = new Button
            {
                Text = "▶",
                Location = new Point(225, 48),
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.System
            };
            _btnIntervalUp.Click += (s, e) => _engine.IncreaseInterval();
            this.Controls.Add(_btnIntervalUp);

            // Line 3: '횟수'
            Label lblCountTitle = new Label
            {
                Text = "횟수",
                Location = new Point(16, 88),
                Size = new Size(38, 20),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33)
            };
            this.Controls.Add(lblCountTitle);

            _btnCountDown = new Button
            {
                Text = "◀",
                Location = new Point(60, 84),
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.System
            };
            _btnCountDown.Click += (s, e) => _engine.DecreaseCount();
            this.Controls.Add(_btnCountDown);

            _lblCount = new Label
            {
                Text = "2회 (최대 100회)",
                Location = new Point(96, 88),
                Size = new Size(125, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular),
                ForeColor = Color.FromArgb(21, 101, 192)
            };
            this.Controls.Add(_lblCount);

            _btnCountUp = new Button
            {
                Text = "▶",
                Location = new Point(225, 84),
                Size = new Size(32, 26),
                FlatStyle = FlatStyle.System
            };
            _btnCountUp.Click += (s, e) => _engine.IncreaseCount();
            this.Controls.Add(_btnCountUp);

            // Line 4: '기능 활성화' 체크박스 & '닫기' 버튼
            _chkEnabled = new CheckBox
            {
                Text = "기능 활성화",
                Location = new Point(18, 122),
                Size = new Size(105, 24),
                Checked = _engine.IsEnabled,
                Font = new Font("Malgun Gothic", 9.0f, FontStyle.Regular)
            };
            _chkEnabled.CheckedChanged += (s, e) =>
            {
                if (_chkEnabled.Checked != _engine.IsEnabled)
                {
                    _engine.ToggleEnabled();
                }
            };
            this.Controls.Add(_chkEnabled);

            _btnClose = new Button
            {
                Text = "닫기",
                Location = new Point(210, 120),
                Size = new Size(62, 28),
                FlatStyle = FlatStyle.System
            };
            _btnClose.Click += (s, e) => this.Hide();
            this.Controls.Add(_btnClose);

            // 1px Border drawing
            this.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(170, 185, 205), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
                }
            };
        }

        private void EnableWindowDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Win32Api.ReleaseCapture();
                Win32Api.SendMessage(this.Handle, Win32Api.WM_NCLBUTTONDOWN, Win32Api.HT_CAPTION, 0);
            }
        }

        private void Engine_SettingsChanged(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUiState));
            }
            else
            {
                UpdateUiState();
            }
        }

        private void Engine_StateChanged(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateUiState));
            }
            else
            {
                UpdateUiState();
            }
        }

        private void UpdateUiState()
        {
            // Line 1: Header
            _lblHeader.Text = $"예상 잠금 : {_engine.TotalExpectedMinutes}분 후";

            // Line 2: Interval
            _lblInterval.Text = $"{_engine.IntervalMinutes}분 (최대 {_engine.MaxInterval}분)";
            _btnIntervalUp.Enabled = _engine.CanIncreaseInterval;
            _btnIntervalDown.Enabled = _engine.CanDecreaseInterval;

            // Line 3: Count
            _lblCount.Text = $"{_engine.MaxSignalCount}회 (최대 {ExtenderEngine.MaxCount}회)";
            _btnCountUp.Enabled = _engine.CanIncreaseCount;
            _btnCountDown.Enabled = _engine.CanDecreaseCount;

            // Line 4: Checkbox
            _chkEnabled.Checked = _engine.IsEnabled;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _engine.SettingsChanged -= Engine_SettingsChanged;
                _engine.StateChanged -= Engine_StateChanged;
            }
            base.Dispose(disposing);
        }
    }
}
