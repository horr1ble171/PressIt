using WindowsInput;
using WindowsInput.Native;

namespace PressIt.Services;

public class KeyboardService
{
    private readonly InputSimulator _simulator = new();
    private readonly HashSet<VirtualKeyCode> _heldKeys = new();

    public IReadOnlySet<VirtualKeyCode> HeldKeys => _heldKeys;

    public void HoldKey(VirtualKeyCode key)
    {
        if (_heldKeys.Add(key))
            _simulator.Keyboard.KeyDown(key);
    }

    public void ReleaseKey(VirtualKeyCode key)
    {
        if (_heldKeys.Remove(key))
            _simulator.Keyboard.KeyUp(key);
    }

    public void TapKey(VirtualKeyCode key)
    {
        _simulator.Keyboard.KeyPress(key);
    }

    public void ReleaseAll()
    {
        foreach (var key in _heldKeys.ToList())
            _simulator.Keyboard.KeyUp(key);
        _heldKeys.Clear();
    }
}
