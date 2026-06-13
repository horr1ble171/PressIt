using PressIt.Services;
using WindowsInput.Native;

namespace PressIt;

public class SimpleHoldControl : UserControl
{
    private readonly KeyboardService _keyboard;
    private readonly Action<string> _updateStatus;
    private readonly Action _emergencyStop;

    private ComboBox _keyCombo;
    private CheckBox _infiniteCheck;
    private NumericUpDown _durationUpDown;
    private Button _startBtn;
    private Button _stopBtn;
    private Label _countdownLabel;
    private System.Windows.Forms.Timer _countdownTimer;
    private System.Windows.Forms.Timer _holdTimer;
    private int _remainingSeconds;

    private VirtualKeyCode? _selectedKey;
    private bool _isHolding;

    public SimpleHoldControl(KeyboardService keyboard, Action<string> updateStatus, Action emergencyStop)
    {
        _keyboard = keyboard;
        _updateStatus = updateStatus;
        _emergencyStop = emergencyStop;

        InitializeControls();
    }

    private void InitializeControls()
    {
        var topLabel = new Label
        {
            Text = "Выберите клавишу (из списка или нажмите):",
            Location = new Point(12, 10),
            Size = new Size(300, 20)
        };

        _keyCombo = new ComboBox
        {
            Location = new Point(12, 35),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;

        _durationUpDown = new NumericUpDown
        {
            Location = new Point(12, 80),
            Size = new Size(80, 25),
            Minimum = 1,
            Maximum = 9999,
            Value = 10
        };

        var secLabel = new Label
        {
            Text = "секунд",
            Location = new Point(100, 82),
            Size = new Size(50, 20)
        };

        _infiniteCheck = new CheckBox
        {
            Text = "Бесконечно (до Стоп)",
            Location = new Point(12, 105),
            Size = new Size(180, 25)
        };
        _infiniteCheck.CheckedChanged += (_, _) => _durationUpDown.Enabled = !_infiniteCheck.Checked;

        _startBtn = new Button
        {
            Text = "Старт",
            Location = new Point(12, 140),
            Size = new Size(100, 35),
            BackColor = Color.LightGreen,
            FlatStyle = FlatStyle.Flat
        };
        _startBtn.FlatAppearance.BorderSize = 0;
        _startBtn.Click += StartHold;

        _stopBtn = new Button
        {
            Text = "Стоп",
            Location = new Point(120, 140),
            Size = new Size(100, 35),
            BackColor = Color.LightCoral,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _stopBtn.FlatAppearance.BorderSize = 0;
        _stopBtn.Click += StopHold;

        _countdownLabel = new Label
        {
            Text = "",
            Location = new Point(12, 190),
            Size = new Size(200, 30),
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.DarkBlue
        };

        Controls.AddRange(new Control[] { topLabel, _keyCombo, _durationUpDown, secLabel, _infiniteCheck, _startBtn, _stopBtn, _countdownLabel });

        _holdTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _holdTimer.Tick += HoldTick;

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += CountdownTick;
    }

    private void StartHold(object? sender, EventArgs e)
    {
        var keyName = _keyCombo.SelectedItem?.ToString();
        if (keyName == null) return;

        _selectedKey = KeyHelper.Map.GetValueOrDefault(keyName);
        if (_selectedKey == null) return;

        _isHolding = true;

        _updateStatus($"Удержание {keyName}");

        if (!_infiniteCheck.Checked)
        {
            _remainingSeconds = (int)_durationUpDown.Value;
            _countdownLabel.Text = $"{_remainingSeconds}с";
            _countdownTimer.Start();
        }
        else
        {
            _countdownLabel.Text = "∞";
        }

        _startBtn.Enabled = false;
        _stopBtn.Enabled = true;
        _keyCombo.Enabled = false;

        this.FindForm()!.WindowState = FormWindowState.Minimized;

        this.BeginInvoke(() =>
        {
            _keyboard.HoldKey(_selectedKey.Value);
            _holdTimer.Start();
        });
    }

    private void StopHold(object? sender, EventArgs e)
    {
        Stop();
    }

    public void Stop()
    {
        _holdTimer.Stop();
        _countdownTimer.Stop();
        if (_isHolding && _selectedKey.HasValue)
        {
            _keyboard.ReleaseKey(_selectedKey.Value);
            _isHolding = false;
        }

        _countdownLabel.Text = "";
        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
        _keyCombo.Enabled = true;

        _updateStatus("Готово");

        this.FindForm()!.WindowState = FormWindowState.Normal;
    }

    private void HoldTick(object? sender, EventArgs e)
    {
        if (_selectedKey.HasValue)
            _keyboard.PressKeyDown(_selectedKey.Value);
    }

    private void CountdownTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        _countdownLabel.Text = $"{_remainingSeconds}с";

        if (_remainingSeconds <= 0)
        {
            Stop();
        }
    }
}
