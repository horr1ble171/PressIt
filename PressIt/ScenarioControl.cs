using System.ComponentModel;
using System.Linq;
using PressIt.Models;
using PressIt.Services;

namespace PressIt;

public class ScenarioControl : UserControl
{
    private readonly KeyboardService _keyboard;
    private readonly ScenarioRunner _runner;
    private readonly Action<string> _updateStatus;

    private ListBox _actionListBox;
    private NumericUpDown _hoursUpDown;
    private NumericUpDown _minutesUpDown;
    private NumericUpDown _secondsUpDown;
    private NumericUpDown _msUpDown;
    private ComboBox _actionTypeCombo;
    private ComboBox _keyCombo;
    private Button _addBtn;
    private Button _removeBtn;
    private Button _startBtn;
    private Button _stopBtn;

    private readonly BindingList<ScenarioAction> _actions = new();
    private CheckBox _loopCheckBox;
    private NumericUpDown _loopCountUpDown;

    public ScenarioControl(KeyboardService keyboard, ScenarioRunner runner, Action<string> updateStatus)
    {
        _keyboard = keyboard;
        _runner = runner;
        _updateStatus = updateStatus;

        InitializeControls();
    }

    private void InitializeControls()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12, 8, 12, 8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Height = 30
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.Controls.Add(new Label { Text = "Действия:", Anchor = AnchorStyles.Left }, 0, 0);
        _removeBtn = new Button
        {
            Text = "Удалить", Size = new Size(90, 24), Anchor = AnchorStyles.Right, Enabled = false
        };
        _removeBtn.Click += RemoveAction;
        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        rightPanel.Controls.Add(new PictureBox
        {
            Image = AppResources.Logo, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(32, 30)
        });
        rightPanel.Controls.Add(_removeBtn);
        headerLayout.Controls.Add(rightPanel, 2, 0);
        layout.Controls.Add(headerLayout, 0, 0);

        _actionListBox = new ListBox { Dock = DockStyle.Fill, DataSource = _actions, IntegralHeight = false };
        _actionListBox.SelectedIndexChanged += (_, _) =>
            _removeBtn.Enabled = _actionListBox.SelectedItem != null;
        layout.Controls.Add(_actionListBox, 0, 1);

        var addGroup = new GroupBox
        {
            Text = "Новое действие",
            Height = 75,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var delayFlow = new FlowLayoutPanel
        {
            Location = new Point(8, 20),
            Size = new Size(500, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        delayFlow.Controls.Add(new Label { Text = "Задержка:", TextAlign = ContentAlignment.MiddleLeft });
        _hoursUpDown = new NumericUpDown { Minimum = 0, Maximum = 999, Width = 50, Value = 0 };
        delayFlow.Controls.Add(_hoursUpDown);
        delayFlow.Controls.Add(new Label { Text = ":", TextAlign = ContentAlignment.MiddleCenter });
        _minutesUpDown = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 50, Value = 0 };
        delayFlow.Controls.Add(_minutesUpDown);
        delayFlow.Controls.Add(new Label { Text = ":", TextAlign = ContentAlignment.MiddleCenter });
        _secondsUpDown = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 50, Value = 1 };
        delayFlow.Controls.Add(_secondsUpDown);
        delayFlow.Controls.Add(new Label { Text = ".", TextAlign = ContentAlignment.MiddleCenter });
        _msUpDown = new NumericUpDown { Minimum = 0, Maximum = 999, Width = 60, Value = 0 };
        delayFlow.Controls.Add(_msUpDown);
        addGroup.Controls.Add(delayFlow);

        var actionFlow = new FlowLayoutPanel
        {
            Location = new Point(8, 47),
            Size = new Size(500, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        actionFlow.Controls.Add(new Label { Text = "Тип:", TextAlign = ContentAlignment.MiddleLeft });
        _actionTypeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 105 };
        _actionTypeCombo.Items.AddRange(new[] { ScenarioAction.GetActionTypeDisplayName(ActionType.Press), ScenarioAction.GetActionTypeDisplayName(ActionType.Release), ScenarioAction.GetActionTypeDisplayName(ActionType.Tap) });
        _actionTypeCombo.SelectedIndex = 0;
        actionFlow.Controls.Add(_actionTypeCombo);

        actionFlow.Controls.Add(new Label { Text = "Клавиша:", TextAlign = ContentAlignment.MiddleLeft });
        _keyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95 };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;
        actionFlow.Controls.Add(_keyCombo);

        _addBtn = new Button { Text = "Добавить", Width = 100 };
        _addBtn.Click += AddAction;
        actionFlow.Controls.Add(_addBtn);

        addGroup.Controls.Add(actionFlow);
        layout.Controls.Add(addGroup, 0, 2);

        var loopPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 30 };
        _loopCheckBox = new CheckBox { Text = "Зациклить", AutoSize = true };
        loopPanel.Controls.Add(_loopCheckBox);
        _loopCountUpDown = new NumericUpDown { Minimum = 0, Maximum = 9999, Width = 60, Value = 1, Enabled = false };
        loopPanel.Controls.Add(_loopCountUpDown);
        loopPanel.Controls.Add(new Label { Text = "раз (0 = беск.)", AutoSize = true });
        _loopCheckBox.CheckedChanged += (_, _) => _loopCountUpDown.Enabled = _loopCheckBox.Checked;
        layout.Controls.Add(loopPanel, 0, 3);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, MinimumSize = new Size(0, 55)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

        _startBtn = new Button
        {
            Text = "Запустить", Dock = DockStyle.Fill, BackColor = Color.LightGreen, FlatStyle = FlatStyle.Flat
        };
        _startBtn.FlatAppearance.BorderSize = 0;
        _startBtn.Click += StartScenario;
        bottomPanel.Controls.Add(_startBtn, 0, 0);

        _stopBtn = new Button
        {
            Text = "Остановить", Dock = DockStyle.Fill, BackColor = Color.LightCoral, FlatStyle = FlatStyle.Flat, Enabled = false
        };
        _stopBtn.FlatAppearance.BorderSize = 0;
        _stopBtn.Click += (_, _) =>
        {
            _runner.Stop();
            _startBtn.Enabled = true;
            _stopBtn.Enabled = false;
            if (this.FindForm() is MainForm main)
                main.RestoreForm();
        };
        bottomPanel.Controls.Add(_stopBtn, 1, 0);

        var saveBtn = new Button { Text = "Сохр.", Dock = DockStyle.Fill };
        saveBtn.Click += SaveScenario;
        bottomPanel.Controls.Add(saveBtn, 2, 0);

        var loadBtn = new Button { Text = "Загр.", Dock = DockStyle.Fill };
        loadBtn.Click += LoadScenario;
        bottomPanel.Controls.Add(loadBtn, 3, 0);

        layout.Controls.Add(bottomPanel, 0, 4);
        Controls.Add(layout);
    }

    private void AddAction(object? sender, EventArgs e)
    {
        var delayMs = ((int)_hoursUpDown.Value * 3600 +
                       (int)_minutesUpDown.Value * 60 +
                       (int)_secondsUpDown.Value) * 1000 +
                       (int)_msUpDown.Value;
        var action = new ScenarioAction
        {
            DelayMs = delayMs,
            ActionType = ScenarioAction.ParseActionType(_actionTypeCombo.SelectedItem?.ToString() ?? "Нажать"),
            Key = _keyCombo.SelectedItem?.ToString() ?? "Space"
        };
        _actions.Add(action);
    }

    private void RemoveAction(object? sender, EventArgs e)
    {
        if (_actionListBox.SelectedItem is ScenarioAction action)
            _actions.Remove(action);
    }

    private async void StartScenario(object? sender, EventArgs e)
    {
        if (_actions.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одно действие в сценарий.", "Нет действий", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var loopCount = _loopCheckBox.Checked ? (int)_loopCountUpDown.Value : 1;
        if (_loopCheckBox.Checked && loopCount == 0)
            loopCount = -1; // infinite

        _startBtn.Enabled = false;
        _stopBtn.Enabled = true;

        var form = this.FindForm()!;
        form.Hide();
        await Task.Delay(200);

        var list = _actions.ToList();
        await _runner.RunAsync(list, loopCount);

        if (this.FindForm() is MainForm main)
            main.RestoreForm();
        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
    }

    public void TryStop()
    {
        if (!_runner.IsRunning) return;
        _runner.Stop();
        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
        if (this.FindForm() is MainForm main)
            main.RestoreForm();
    }

    public void TryStart()
    {
        if (_runner.IsRunning) return;
        if (_actions.Count == 0)
        {
            _updateStatus("Нет действий в сценарии");
            return;
        }
        StartScenario(null, EventArgs.Empty);
    }

    public void Stop()
    {
        _runner.Stop();
        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
        if (this.FindForm() is MainForm main)
            main.RestoreForm();
    }

    private void SaveScenario(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "scenario.json" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_actions.ToList(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
        }
    }

    private void LoadScenario(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var json = File.ReadAllText(dialog.FileName);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<List<ScenarioAction>>(json);
            if (loaded != null)
            {
                _actions.Clear();
                foreach (var a in loaded)
                    _actions.Add(a);
            }
        }
    }
}
