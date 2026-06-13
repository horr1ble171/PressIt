using WindowsInput.Native;

namespace PressIt;

public static class KeyHelper
{
    public static readonly Dictionary<string, VirtualKeyCode> Map = new()
    {
        { "W", VirtualKeyCode.VK_W },
        { "A", VirtualKeyCode.VK_A },
        { "S", VirtualKeyCode.VK_S },
        { "D", VirtualKeyCode.VK_D },
        { "X", VirtualKeyCode.VK_X },
        { "Space", VirtualKeyCode.SPACE },
        { "Ctrl", VirtualKeyCode.CONTROL },
        { "Shift", VirtualKeyCode.SHIFT },
        { "Alt", VirtualKeyCode.MENU },
        { "Enter", VirtualKeyCode.RETURN },
        { "E", VirtualKeyCode.VK_E },
        { "Q", VirtualKeyCode.VK_Q },
        { "R", VirtualKeyCode.VK_R },
        { "F", VirtualKeyCode.VK_F },
        { "Z", VirtualKeyCode.VK_Z },
        { "C", VirtualKeyCode.VK_C },
        { "V", VirtualKeyCode.VK_V },
        { "B", VirtualKeyCode.VK_B },
        { "Tab", VirtualKeyCode.TAB },
        { "Esc", VirtualKeyCode.ESCAPE },
        { "Up", VirtualKeyCode.UP },
        { "Down", VirtualKeyCode.DOWN },
        { "Left", VirtualKeyCode.LEFT },
        { "Right", VirtualKeyCode.RIGHT },
        { "1", VirtualKeyCode.VK_1 },
        { "2", VirtualKeyCode.VK_2 },
        { "3", VirtualKeyCode.VK_3 },
        { "4", VirtualKeyCode.VK_4 },
        { "5", VirtualKeyCode.VK_5 },
        { "0", VirtualKeyCode.VK_0 },
    };
}
