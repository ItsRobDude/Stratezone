using Godot;

public partial class MissionBriefingOverlay : Panel
{
    private readonly Label _title = new();
    private readonly Label _body = new();
    private readonly Label _tactical = new();
    private readonly Label _footer = new();
    private float _uiScale = 1.0f;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Pass;

        _title.ThemeTypeVariation = "LabelTitle";
        _title.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_title);

        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_body);

        _tactical.ThemeTypeVariation = "LabelSecondary";
        _tactical.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_tactical);

        _footer.ThemeTypeVariation = "LabelSecondary";
        _footer.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_footer);
    }

    public void ApplyUiScale(float uiScale, Vector2 viewportSize)
    {
        _uiScale = uiScale;
        var width = Mathf.Min(720.0f * uiScale, viewportSize.X - (48.0f * uiScale));
        var height = Mathf.Min(270.0f * uiScale, viewportSize.Y - (96.0f * uiScale));
        AnchorLeft = 0.5f;
        AnchorTop = 0.5f;
        AnchorRight = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = width * -0.5f;
        OffsetTop = height * -0.5f;
        OffsetRight = width * 0.5f;
        OffsetBottom = height * 0.5f;

        var pad = 24.0f * uiScale;
        _title.Position = new Vector2(pad, 18.0f * uiScale);
        _title.Size = new Vector2(width - (pad * 2.0f), 30.0f * uiScale);
        _body.Position = new Vector2(pad, 58.0f * uiScale);
        _body.Size = new Vector2(width - (pad * 2.0f), 104.0f * uiScale);
        _tactical.Position = new Vector2(pad, 166.0f * uiScale);
        _tactical.Size = new Vector2(width - (pad * 2.0f), 48.0f * uiScale);
        _footer.Position = new Vector2(pad, height - (36.0f * uiScale));
        _footer.Size = new Vector2(width - (pad * 2.0f), 18.0f * uiScale);
    }

    public void UpdateBriefing(string title, string body, string tactical, string footer)
    {
        _title.Text = title;
        _body.Text = body;
        _tactical.Text = tactical;
        _footer.Text = footer;
    }
}
