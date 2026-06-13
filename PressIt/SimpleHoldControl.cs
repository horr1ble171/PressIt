using MaterialSkin.Controls;
using PressIt.Services;
using WindowsInput.Native;

namespace PressIt;

public class SimpleHoldControl : UserControl
{
    private readonly KeyboardService _keyboard;
    private readonly Action<string> _updateStatus;
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

    public SimpleHoldControl(KeyboardService keyboard, Action<string> updateStatus)
    {
        _keyboard = keyboard;
        _updateStatus = updateStatus;

        InitializeControls();
    }

    private void InitializeControls()
    {
        var topLabel = new Label
        {
            Text = "Выберите клавишу (из списка или нажмите):",
            Location = new Point(30, 30),
            AutoSize = true
        };

        _keyCombo = new MaterialComboBox
        {
            Location = new Point(30, 55),
            Size = new Size(200, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoResize = false
        };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;

        _durationUpDown = new NumericUpDown
        {
            Location = new Point(30, 120),
            Size = new Size(110, 26),
            Minimum = 1,
            Maximum = 9999,
            Value = 10,
            TextAlign = HorizontalAlignment.Center
        };

        var secLabel = new Label
        {
            Text = "секунд",
            Location = new Point(145, 122),
            AutoSize = true
        };

        _infiniteCheck = new MaterialCheckbox
        {
            Text = "Бесконечно (до Стоп)",
            Location = new Point(30, 160),
            Size = new Size(240, 30)
        };
        _infiniteCheck.CheckedChanged += (_, _) => _durationUpDown.Enabled = !_infiniteCheck.Checked;

        _startBtn = new MaterialButton
        {
            Text = "Старт [F6]",
            Location = new Point(30, 195),
            Size = new Size(220, 55),
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
        };
        _startBtn.Click += StartHold;

        _stopBtn = new MaterialButton
        {
            Text = "Стоп [F7]",
            Location = new Point(270, 195),
            Size = new Size(220, 55),
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Enabled = false
        };

        _countdownLabel = new Label
        {
            Text = "",
            Location = new Point(30, 280),
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.DarkBlue
        };

        Controls.AddRange(new Control[] { topLabel, _keyCombo, _durationUpDown, secLabel, _infiniteCheck, _startBtn, _stopBtn, _countdownLabel });

        var tooltip = new ToolTip();
        tooltip.SetToolTip(_startBtn, "Глобальная горячая клавиша: F6");
        tooltip.SetToolTip(_stopBtn, "Глобальная горячая клавиша: F7");

        _holdTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _holdTimer.Tick += HoldTick;

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += CountdownTick;
    }

    public void TryStart()
    {
        if (_isHolding) return;
        StartHold(null, EventArgs.Empty);
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
