using System.Collections.Generic;

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
        var totalMs = DelayMs;
        var hours = totalMs / 3600000;
        totalMs %= 3600000;
        var minutes = totalMs / 60000;
        totalMs %= 60000;
        var seconds = totalMs / 1000;
        var ms = totalMs % 1000;

        var parts = new List<string>();
        if (hours > 0) parts.Add($"{hours}ч");
        if (minutes > 0) parts.Add($"{minutes}м");
        if (seconds > 0) parts.Add($"{seconds}с");
        if (ms > 0 || parts.Count == 0) parts.Add($"{ms}мс");

        return $"+{string.Join(" ", parts)} [{GetActionTypeDisplayName(ActionType)}] {Key}";
    }
}
