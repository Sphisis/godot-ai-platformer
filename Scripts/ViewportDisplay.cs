using Godot;

public partial class ViewportDisplay : Godot.TextureRect
{
    [Export]
    public NodePath SubViewportPath { get; set; }

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
        
        GD.Print("[ViewportDisplay] Successfully connected viewport texture");
    }
}