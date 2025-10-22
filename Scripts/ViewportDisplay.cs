using Godot;

public partial class ViewportDisplay : Godot.TextureRect
{
    [Export]
    public NodePath SubViewportPath { get; set; }

    [Export(PropertyHint.Range, "0.1,5.0,0.1")]
    public float FadeInDuration { get; set; } = 1.0f;

    public override void _Ready()
    {
        // Get the SubViewport node
        var viewport = GetNode(SubViewportPath) as SubViewport;
        if (viewport == null)
        {
            GD.PushWarning($"[ViewportDisplay] No SubViewport found at path: {SubViewportPath}");
            return;
        }

        // Set up the viewport for texture output
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        viewport.RenderTargetClearMode = SubViewport.ClearMode.Always;

        // Get the viewport texture and assign it
        TextureFilter = TextureFilterEnum.Linear;  // Optional: make the texture smoother
        Texture = viewport.GetTexture() as ViewportTexture;
        
        // Start invisible and fade in
        Modulate = new Color(1, 1, 1, 0);
        
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, FadeInDuration);
        
        GD.Print("[ViewportDisplay] Successfully connected viewport texture");
    }
}