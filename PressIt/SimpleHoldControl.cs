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
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(16, 12, 16, 12)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // Row 0: метка "Выберите клавишу"
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Row 1: ComboBox клавиши
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Row 2: длительность (NumericUpDown + "секунд")
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Row 3: чекбокс "Бесконечно"
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Row 4: кнопки Старт / Стоп (фиксированная высота)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        // Row 5: countdown
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // --- Строка 0: Заголовок ---
        var topLabel = new Label
        {
            Text = "Выберите клавишу (из списка или нажмите):",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 0, 4)
        };
        mainLayout.Controls.Add(topLabel, 0, 0);

        // --- Строка 1: ComboBox ---
        _keyCombo = new MaterialComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoResize = false,
            Margin = new Padding(0, 0, 0, 10)
        };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;
        mainLayout.Controls.Add(_keyCombo, 0, 1);

        // --- Строка 2: Длительность ---
        var durationFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 4)
        };
        _durationUpDown = new NumericUpDown
        {
            Minimum = 1, Maximum = 9999, Value = 10,
            TextAlign = HorizontalAlignment.Center,
            Width = 80,
            Margin = new Padding(0, 0, 6, 0)
        };
        durationFlow.Controls.Add(_durationUpDown);
        durationFlow.Controls.Add(new Label
        {
            Text = "секунд",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 0, 0)
        });
        mainLayout.Controls.Add(durationFlow, 0, 2);

        // --- Строка 3: Чекбокс ---
        _infiniteCheck = new MaterialCheckbox
        {
            Text = "Бесконечно (до Стоп)",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 0, 10)
        };
        _infiniteCheck.CheckedChanged += (_, _) => _durationUpDown.Enabled = !_infiniteCheck.Checked;
        mainLayout.Controls.Add(_infiniteCheck, 0, 3);

        // --- Строка 4: Кнопки Старт / Стоп ---
        var btnLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 8)
        };
        btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _startBtn = new MaterialButton
        {
            Text = "Старт [F6]",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            Margin = new Padding(0, 0, 4, 0)
        };
        _startBtn.Click += StartHold;
        btnLayout.Controls.Add(_startBtn, 0, 0);

        _stopBtn = new MaterialButton
        {
            Text = "Стоп [F7]",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            Enabled = false,
            Margin = new Padding(4, 0, 0, 0)
        };
        _stopBtn.Click += StopHold;
        btnLayout.Controls.Add(_stopBtn, 1, 0);
        mainLayout.Controls.Add(btnLayout, 0, 4);

        // --- Строка 5: Countdown ---
        _countdownLabel = new Label
        {
            Text = "",
            AutoSize = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 40,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.DarkBlue
        };
        mainLayout.Controls.Add(_countdownLabel, 0, 5);

        Controls.Add(mainLayout);

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

    private void StopHold(object? sender, EventArgs e) => Stop();

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
        if (_remainingSeconds <= 0) Stop();
    }
}
