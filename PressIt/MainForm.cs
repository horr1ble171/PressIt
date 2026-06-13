using System.Runtime.InteropServices;
using PressIt.Services;
using WindowsInput.Native;

namespace PressIt;

public partial class MainForm : Form
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_START = 1;
    private const int HOTKEY_ID_STOP = 2;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private class HotkeyFilter : IMessageFilter
    {
        private readonly MainForm _form;
        public HotkeyFilter(MainForm form) => _form = form;
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                var id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_START)
                {
                    _form._tabControl.SelectedTab = _form._tabScenario;
                    _form._scenarioControl?.TryStart();
                }
                else if (id == HOTKEY_ID_STOP)
                {
                    _form.EmergencyStop();
                }
                return true;
            }
            return false;
        }
    }

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
        MinimumSize = new Size(560, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        using var logoBmp = new Bitmap(AppResources.Logo, 32, 32);
        Icon = Icon.FromHandle(logoBmp.GetHicon());

        _runner = new ScenarioRunner(_keyboard);
        _runner.StatusChanged += msg => BeginInvoke(() => _statusLabel.Text = msg);

        InitializeControls();

        Application.AddMessageFilter(new HotkeyFilter(this));
        RegisterHotKey(IntPtr.Zero, HOTKEY_ID_START, 0, (int)Keys.F6);
        RegisterHotKey(IntPtr.Zero, HOTKEY_ID_STOP, 0, (int)Keys.F7);
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
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_START);
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_STOP);
        _keyboard.ReleaseAll();
        base.OnFormClosing(e);
    }
}
