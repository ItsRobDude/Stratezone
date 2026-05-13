public sealed record CommandPanelAction(
    string Name,
    string Hotkey,
    string Tooltip,
    bool Enabled,
    Action Execute,
    string IconId = "",
    string Cost = "",
    bool Active = false
);
