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
    private NumericUpDown _delayUpDown;
    private ComboBox _actionTypeCombo;
    private ComboBox _keyCombo;
    private Button _addBtn;
    private Button _removeBtn;
    private Button _startBtn;
    private Button _stopBtn;

    private readonly BindingList<ScenarioAction> _actions = new();

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
            RowCount = 4,
            Padding = new Padding(12, 8, 12, 8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerPanel = new Panel { Height = 30 };
        headerPanel.Controls.Add(new Label
        {
            Text = "Действия:",
            Location = new Point(0, 6),
            AutoSize = true
        });
        _removeBtn = new Button
        {
            Text = "Удалить",
            Location = new Point(0, 3),
            Size = new Size(90, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false
        };
        _removeBtn.Click += RemoveAction;
        headerPanel.Controls.Add(_removeBtn);
        headerPanel.Controls.Add(new PictureBox
        {
            Image = AppResources.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(0, 0),
            Size = new Size(32, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        });
        layout.Controls.Add(headerPanel, 0, 0);

        _actionListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DataSource = _actions
        };
        _actionListBox.SelectedIndexChanged += (_, _) =>
            _removeBtn.Enabled = _actionListBox.SelectedItem != null;
        layout.Controls.Add(_actionListBox, 0, 1);

        var addGroup = new GroupBox
        {
            Text = "Новое действие",
            Dock = DockStyle.Fill
        };
        var innerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            Padding = new Padding(8, 20, 8, 4)
        };
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        innerLayout.Controls.Add(new Label { Text = "Задержка:", Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _delayUpDown = new NumericUpDown { Minimum = 0, Maximum = 60000, Value = 1000 };
        innerLayout.Controls.Add(_delayUpDown, 1, 0);

        innerLayout.Controls.Add(new Label { Text = "Тип:", Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        _actionTypeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _actionTypeCombo.Items.AddRange(new[] { ScenarioAction.GetActionTypeDisplayName(ActionType.Press), ScenarioAction.GetActionTypeDisplayName(ActionType.Release), ScenarioAction.GetActionTypeDisplayName(ActionType.Tap) });
        _actionTypeCombo.SelectedIndex = 0;
        innerLayout.Controls.Add(_actionTypeCombo, 3, 0);

        innerLayout.Controls.Add(new Label { Text = "Клавиша:", Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 4, 0);
        _keyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;
        innerLayout.Controls.Add(_keyCombo, 5, 0);

        _addBtn = new Button { Text = "Добавить", Dock = DockStyle.Fill };
        _addBtn.Click += AddAction;
        innerLayout.Controls.Add(_addBtn, 6, 0);

        addGroup.Controls.Add(innerLayout);
        layout.Controls.Add(addGroup, 0, 2);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            MinimumSize = new Size(0, 55)
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));

        _startBtn = new Button
        {
            Text = "Запустить",
            Dock = DockStyle.Fill,
            BackColor = Color.LightGreen,
            FlatStyle = FlatStyle.Flat
        };
        _startBtn.FlatAppearance.BorderSize = 0;
        _startBtn.Click += StartScenario;
        bottomPanel.Controls.Add(_startBtn, 0, 0);

        _stopBtn = new Button
        {
            Text = "Остановить",
            Dock = DockStyle.Fill,
            BackColor = Color.LightCoral,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _stopBtn.FlatAppearance.BorderSize = 0;
        _stopBtn.Click += (_, _) => _runner.Stop();
        bottomPanel.Controls.Add(_stopBtn, 1, 0);

        var saveBtn = new Button { Text = "Сохр.", Dock = DockStyle.Fill };
        saveBtn.Click += SaveScenario;
        bottomPanel.Controls.Add(saveBtn, 2, 0);

        var loadBtn = new Button { Text = "Загр.", Dock = DockStyle.Fill };
        loadBtn.Click += LoadScenario;
        bottomPanel.Controls.Add(loadBtn, 3, 0);

        layout.Controls.Add(bottomPanel, 0, 3);
        Controls.Add(layout);
    }

    private void AddAction(object? sender, EventArgs e)
    {
        var action = new ScenarioAction
        {
            DelayMs = (int)_delayUpDown.Value,
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

        _startBtn.Enabled = false;
        _stopBtn.Enabled = true;

        var list = _actions.ToList();
        await _runner.RunAsync(list);

        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
    }

    public void Stop()
    {
        _runner.Stop();
        _startBtn.Enabled = true;
        _stopBtn.Enabled = false;
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
