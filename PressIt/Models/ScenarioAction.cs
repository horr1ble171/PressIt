namespace PressIt.Models;

public enum ActionType
{
    Press,
    Release,
    Tap
}

public class ScenarioAction
{
    public ActionType ActionType { get; set; }
    public string Key { get; set; } = string.Empty;
    public int DelayMs { get; set; }

    public override string ToString()
    {
        return $"+{DelayMs}ms [{ActionType}] {Key}";
    }
}
