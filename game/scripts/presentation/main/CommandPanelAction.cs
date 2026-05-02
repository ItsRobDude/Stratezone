public sealed record CommandPanelAction(
    string Label,
    string Hint,
    bool Enabled,
    Action Execute,
    string Icon = "",
    string Cost = ""
);
