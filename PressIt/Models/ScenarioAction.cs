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

    public static string GetActionTypeDisplayName(ActionType type) => type switch
    {
        ActionType.Press => "Зажать",
        ActionType.Release => "Отпустить",
        ActionType.Tap => "Нажать",
        _ => type.ToString()
    };

    public static ActionType ParseActionType(string displayName) => displayName switch
    {
        "Зажать" => ActionType.Press,
        "Отпустить" => ActionType.Release,
        _ => ActionType.Tap
    };

    public override string ToString()
    {
        return $"+{DelayMs}мс [{GetActionTypeDisplayName(ActionType)}] {Key}";
    }
}
