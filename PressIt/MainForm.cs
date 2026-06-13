using System.Runtime.InteropServices;
using MaterialSkin;
using MaterialSkin.Controls;
using PressIt.Services;
using WindowsInput.Native;

namespace PressIt;

public partial class MainForm : MaterialForm
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private readonly KeyboardService _keyboard = new();
    private readonly ScenarioRunner _runner;
    private readonly System.Windows.Forms.Timer _hotkeyTimer;
    private bool _f6WasDown;
    private bool _f7WasDown;

    private TabControl _tabControl;
    private TabPage _tabSimple;
    private TabPage _tabScenario;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;

    private SimpleHoldControl _simpleHoldControl;
    private ScenarioControl _scenarioControl;
    private Bitmap _logoBmp;

    public MainForm()
    {
        Text = "PressIt";
        ClientSize = new Size(640, 560);
        MinimumSize = new Size(600, 520);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        _logoBmp = new Bitmap(AppResources.Logo, 32, 32);
        Icon = Icon.FromHandle(_logoBmp.GetHicon());
        ShowIcon = true;

        _runner = new ScenarioRunner(_keyboard);
        _runner.StatusChanged += msg => BeginInvoke(() => _statusLabel.Text = msg);

        InitializeControls();

        var skin = MaterialSkinManager.Instance;
        skin.AddFormToManage(this);
        skin.Theme = MaterialSkinManager.Themes.LIGHT;
        skin.ColorScheme = new ColorScheme(
            Primary.BlueGrey800, Primary.BlueGrey900,
            Primary.BlueGrey500, Accent.LightBlue200,
            TextShade.WHITE);

        _hotkeyTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _hotkeyTimer.Tick += PollHotkeys;
        _hotkeyTimer.Start();
    }

    private void PollHotkeys(object? sender, EventArgs e)
    {
        var f6Down = (GetAsyncKeyState((int)Keys.F6) & 0x8000) != 0;
        var f7Down = (GetAsyncKeyState((int)Keys.F7) & 0x8000) != 0;

        if (f6Down && !_f6WasDown)
        {
            if (_tabControl.SelectedTab == _tabScenario)
                _scenarioControl?.TryStart();
            else
                _simpleHoldControl?.TryStart();
        }

        if (f7Down && !_f7WasDown)
            EmergencyStop();

        _f6WasDown = f6Down;
        _f7WasDown = f7Down;
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

    public void RestoreForm()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RestoreForm);
            return;
        }
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _hotkeyTimer.Stop();
        _keyboard.ReleaseAll();
        _logoBmp?.Dispose();
        base.OnFormClosing(e);
    }
}
