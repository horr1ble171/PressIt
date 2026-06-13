using PressIt.Services;
using WindowsInput.Native;

namespace PressIt;

public partial class MainForm : Form
{
    private readonly KeyboardService _keyboard = new();
    private readonly ScenarioRunner _runner;

    private TabControl _tabControl;
    private TabPage _tabSimple;
    private TabPage _tabScenario;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;

    private SimpleHoldControl _simpleHoldControl;
    private ScenarioControl _scenarioControl;

    public MainForm()
    {
        Text = "PressIt";
        ClientSize = new Size(600, 500);
        MinimumSize = new Size(500, 400);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        _runner = new ScenarioRunner(_keyboard);
        _runner.StatusChanged += msg => BeginInvoke(() => _statusLabel.Text = msg);

        InitializeControls();
    }

    private void InitializeControls()
    {
        _tabControl = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(_tabControl);

        _tabSimple = new TabPage("Простое удержание");
        _tabScenario = new TabPage("Сценарий");

        _tabControl.TabPages.Add(_tabSimple);
        _tabControl.TabPages.Add(_tabScenario);

        _simpleHoldControl = new SimpleHoldControl(_keyboard, UpdateStatus)
        {
            Dock = DockStyle.Fill
        };
        _tabSimple.Controls.Add(_simpleHoldControl);

        _scenarioControl = new ScenarioControl(_keyboard, _runner, UpdateStatus)
        {
            Dock = DockStyle.Fill
        };
        _tabScenario.Controls.Add(_scenarioControl);

        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Готово")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(_statusStrip);
    }

    public void UpdateStatus(string text)
    {
        _statusLabel.Text = text;
    }

    public void EmergencyStop()
    {
        _runner.Stop();
        _keyboard.ReleaseAll();
        _simpleHoldControl?.Stop();
        _scenarioControl?.Stop();
        UpdateStatus("Аварийная остановка — все клавиши отпущены");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _keyboard.ReleaseAll();
        base.OnFormClosing(e);
    }
}
