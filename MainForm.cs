using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreensaverExtender
{
    public class MainForm : Form
    {
        private readonly ExtenderEngine _engine;

        private Label _lblInterval = null!;
        private Label _lblIntervalLimit = null!;
        private Button _btnIntervalDown = null!;
        private Button _btnIntervalUp = null!;

        private Label _lblCount = null!;
        private Label _lblCountLimit = null!;
        private Button _btnCountDown = null!;
        private Button _btnCountUp = null!;

        private Label _lblBaseTimeout = null!;
        private Label _lblTotalExpected = null!;
        private Label _lblStatus = null!;
        private CheckBox _chkEnabled = null!;

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
            this.Text = "스마트 화면보호기 지연 설정";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(390, 370);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Regular);
            this.ShowInTaskbar = false;

            // 1. Title / Header Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(30, 136, 229)
            };
            Label titleLabel = new Label
            {
                Text = "⏱ 스마트 화면보호기 지연기",
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 11.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(titleLabel);
            this.Controls.Add(headerPanel);

            // Container Panel
            Panel bodyPanel = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(360, 260),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(bodyPanel);

            int yOffset = 15;

            // Row 1: Interval (주기 설정)
            Label lblIntervalTitle = new Label
            {
                Text = "신호 주기 :",
                Location = new Point(15, yOffset + 4),
                Size = new Size(80, 25),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold)
            };
            bodyPanel.Controls.Add(lblIntervalTitle);

            _btnIntervalDown = new Button
            {
                Text = "◀",
                Location = new Point(100, yOffset),
                Size = new Size(38, 28),
                FlatStyle = FlatStyle.System
            };
            _btnIntervalDown.Click += (s, e) => _engine.DecreaseInterval();
            bodyPanel.Controls.Add(_btnIntervalDown);

            _lblInterval = new Label
            {
                Text = "3 분",
                Location = new Point(145, yOffset + 3),
                Size = new Size(70, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 101, 192)
            };
            bodyPanel.Controls.Add(_lblInterval);

            _btnIntervalUp = new Button
            {
                Text = "▶",
                Location = new Point(220, yOffset),
                Size = new Size(38, 28),
                FlatStyle = FlatStyle.System
            };
            _btnIntervalUp.Click += (s, e) => _engine.IncreaseInterval();
            bodyPanel.Controls.Add(_btnIntervalUp);

            _lblIntervalLimit = new Label
            {
                Text = "(최대 4분)",
                Location = new Point(265, yOffset + 5),
                Size = new Size(80, 20),
                ForeColor = Color.Gray,
                Font = new Font("Malgun Gothic", 8.5f)
            };
            bodyPanel.Controls.Add(_lblIntervalLimit);

            yOffset += 45;

            // Row 2: Count (횟수 설정)
            Label lblCountTitle = new Label
            {
                Text = "연장 횟수 :",
                Location = new Point(15, yOffset + 4),
                Size = new Size(80, 25),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold)
            };
            bodyPanel.Controls.Add(lblCountTitle);

            _btnCountDown = new Button
            {
                Text = "◀",
                Location = new Point(100, yOffset),
                Size = new Size(38, 28),
                FlatStyle = FlatStyle.System
            };
            _btnCountDown.Click += (s, e) => _engine.DecreaseCount();
            bodyPanel.Controls.Add(_btnCountDown);

            _lblCount = new Label
            {
                Text = "2 회",
                Location = new Point(145, yOffset + 3),
                Size = new Size(70, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 101, 192)
            };
            bodyPanel.Controls.Add(_lblCount);

            _btnCountUp = new Button
            {
                Text = "▶",
                Location = new Point(220, yOffset),
                Size = new Size(38, 28),
                FlatStyle = FlatStyle.System
            };
            _btnCountUp.Click += (s, e) => _engine.IncreaseCount();
            bodyPanel.Controls.Add(_btnCountUp);

            _lblCountLimit = new Label
            {
                Text = "(최대 100회)",
                Location = new Point(265, yOffset + 5),
                Size = new Size(85, 20),
                ForeColor = Color.Gray,
                Font = new Font("Malgun Gothic", 8.5f)
            };
            bodyPanel.Controls.Add(_lblCountLimit);

            yOffset += 45;

            // Divider Line
            Label divider = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(15, yOffset),
                Size = new Size(330, 2)
            };
            bodyPanel.Controls.Add(divider);

            yOffset += 10;

            // Row 3: Windows ScreenSaver Info
            _lblBaseTimeout = new Label
            {
                Text = "• PC 기본 화면보호기: 5분 후 작동",
                Location = new Point(15, yOffset),
                Size = new Size(330, 22),
                ForeColor = Color.FromArgb(66, 66, 66)
            };
            bodyPanel.Controls.Add(_lblBaseTimeout);

            yOffset += 24;

            // Row 4: Total Expected Time (주요 강조)
            _lblTotalExpected = new Label
            {
                Text = "▶ 최종 예상 잠금: 약 13분 후 (기본 5분 + 연장 8분)",
                Location = new Point(15, yOffset),
                Size = new Size(330, 24),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(198, 40, 40) // Red/Dark Amber highlight
            };
            bodyPanel.Controls.Add(_lblTotalExpected);

            yOffset += 32;

            // Row 5: Current Status
            _lblStatus = new Label
            {
                Text = "• 상태: 사용자 작업 중 (대기 0/2회)",
                Location = new Point(15, yOffset),
                Size = new Size(330, 40),
                ForeColor = Color.FromArgb(46, 125, 50),
                Font = new Font("Malgun Gothic", 9.0f)
            };
            bodyPanel.Controls.Add(_lblStatus);

            // Bottom Controls (Checkbox & Close Button)
            _chkEnabled = new CheckBox
            {
                Text = "기능 활성화",
                Location = new Point(20, 325),
                Size = new Size(110, 25),
                Checked = _engine.IsEnabled
            };
            _chkEnabled.CheckedChanged += (s, e) =>
            {
                if (_chkEnabled.Checked != _engine.IsEnabled)
                {
                    _engine.ToggleEnabled();
                }
            };
            this.Controls.Add(_chkEnabled);

            Button btnHide = new Button
            {
                Text = "트레이로 닫기",
                Location = new Point(255, 323),
                Size = new Size(120, 30),
                FlatStyle = FlatStyle.System
            };
            btnHide.Click += (s, e) => this.Hide();
            this.Controls.Add(btnHide);

            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
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
            _lblInterval.Text = $"{_engine.IntervalMinutes} 분";
            _lblCount.Text = $"{_engine.MaxSignalCount} 회";
            _chkEnabled.Checked = _engine.IsEnabled;

            // Safe Guard: Toggle arrow buttons availability
            _btnIntervalUp.Enabled = _engine.CanIncreaseInterval;
            _btnIntervalDown.Enabled = _engine.CanDecreaseInterval;
            _btnCountUp.Enabled = _engine.CanIncreaseCount;
            _btnCountDown.Enabled = _engine.CanDecreaseCount;

            _lblIntervalLimit.Text = $"(최대 {_engine.MaxInterval}분)";
            _lblBaseTimeout.Text = $"• PC 기본 화면보호기: {_engine.ScreenSaverTimeoutMinutes}분 후 작동";

            int extensionMinutes = _engine.MaxSignalCount * _engine.IntervalMinutes;
            _lblTotalExpected.Text = $"▶ 최종 예상 잠금: 약 {_engine.TotalExpectedMinutes}분 후 (기본 {_engine.ScreenSaverTimeoutMinutes}분 + 연장 {extensionMinutes}분)";

            _lblStatus.Text = $"• 상태: {_engine.GetStatusDescription()}";
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
