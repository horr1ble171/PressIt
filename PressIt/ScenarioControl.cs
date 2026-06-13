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
        var listLabel = new Label
        {
            Text = "Действия:",
            Location = new Point(15, 10),
            Size = new Size(200, 15)
        };

        _actionListBox = new ListBox
        {
            Location = new Point(15, 28),
            Size = new Size(530, 185),
            DataSource = _actions
        };

        var addGroup = new GroupBox
        {
            Text = "Добавить действие",
            Location = new Point(15, 225),
            Size = new Size(530, 110)
        };

        var delayLabel = new Label { Text = "Задержка (мс):", Location = new Point(10, 25), Size = new Size(85, 15) };
        _delayUpDown = new NumericUpDown { Location = new Point(100, 23), Size = new Size(70, 22), Minimum = 0, Maximum = 60000, Value = 1000 };

        var typeLabel = new Label { Text = "Тип:", Location = new Point(10, 55), Size = new Size(30, 15) };
        _actionTypeCombo = new ComboBox
        {
            Location = new Point(45, 53), Size = new Size(110, 22), DropDownStyle = ComboBoxStyle.DropDownList
        };
        _actionTypeCombo.Items.AddRange(new[] { ScenarioAction.GetActionTypeDisplayName(ActionType.Press), ScenarioAction.GetActionTypeDisplayName(ActionType.Release), ScenarioAction.GetActionTypeDisplayName(ActionType.Tap) });
        _actionTypeCombo.SelectedIndex = 0;

        var keyLabel = new Label { Text = "Клавиша:", Location = new Point(170, 55), Size = new Size(55, 15) };
        _keyCombo = new ComboBox
        {
            Location = new Point(230, 53), Size = new Size(90, 22), DropDownStyle = ComboBoxStyle.DropDownList
        };
        _keyCombo.Items.AddRange(new[] { "W", "A", "S", "D", "X", "Space", "Ctrl", "Shift", "Alt", "Enter", "Q", "E", "R", "F", "Z", "C", "V", "Tab", "Esc", "Up", "Down", "Left", "Right", "1", "2", "3", "4", "5", "0" });
        _keyCombo.SelectedIndex = 0;

        _addBtn = new Button { Text = "Добавить", Location = new Point(380, 22), Size = new Size(135, 28) };
        _addBtn.Click += AddAction;

        _removeBtn = new Button { Text = "Удалить", Location = new Point(380, 55), Size = new Size(135, 28) };
        _removeBtn.Click += RemoveAction;

        addGroup.Controls.AddRange(new Control[] { delayLabel, _delayUpDown, typeLabel, _actionTypeCombo, keyLabel, _keyCombo, _addBtn, _removeBtn });

        _startBtn = new Button
        {
            Text = "Запустить",
            Location = new Point(15, 350),
            Size = new Size(180, 50),
            BackColor = Color.LightGreen,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold)
        };
        _startBtn.FlatAppearance.BorderSize = 0;
        _startBtn.Click += StartScenario;

        _stopBtn = new Button
        {
            Text = "Остановить",
            Location = new Point(205, 350),
            Size = new Size(180, 50),
            BackColor = Color.LightCoral,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            Enabled = false
        };
        _stopBtn.FlatAppearance.BorderSize = 0;
        _stopBtn.Click += (_, _) => _runner.Stop();

        var saveBtn = new Button { Text = "Сохр.", Location = new Point(400, 350), Size = new Size(65, 50) };
        saveBtn.Click += SaveScenario;

        var loadBtn = new Button { Text = "Загр.", Location = new Point(470, 350), Size = new Size(65, 50) };
        loadBtn.Click += LoadScenario;

        Controls.AddRange(new Control[] { listLabel, _actionListBox, addGroup, _startBtn, _stopBtn, saveBtn, loadBtn });
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
