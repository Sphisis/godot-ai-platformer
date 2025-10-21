using Godot;
using System;

public partial class GlichSprite : AnimatedSprite2D
{
	// Sprite effect properties
	private float _effectTime = 0f;

	[ExportGroup("Glitch Effect")]
	[Export(PropertyHint.Range, "0,50,1")] 
	public float FlickerSpeed = 30f;  // How fast the color changes

	[Export(PropertyHint.Range, "0,1,0.1")] 
	public float FlickerChance = 0.5f;  // Chance to change color each frame

	[Export(PropertyHint.Range, "0,1,0.1")] 
	public float FlickerDuration = 0.7f;  // How long each color stays (0-1)

	[Export]
	public Color baseColor = Colors.White;  // Color to flicker to
	
	[Export] 
	public Color GlitchColor = new Color(1, 0, 0, 0.8f);  // Color to flicker to

	[ExportGroup("Scale Effect")]
	[Export(PropertyHint.Range, "0.5,2.0,0.1")] 
	public float MinScale = 1.0f;

	[Export(PropertyHint.Range, "0.5,2.0,0.1")] 
	public float MaxScale = 1.4f;

	[Export(PropertyHint.Range, "0,0.5,0.01")] 
	public float JitterAmount = 0.05f;  // How much random scale jitter to add

	[Export(PropertyHint.Range, "0,1,0.1")]
	public float JumpChance = 0.5f;  // Chance for sudden scale changes

	public override void _Process(double delta)
	{
		// Update the effect time
		_effectTime += (float)GetProcessDeltaTime();

		// Glitchy color using noise and random
		float noiseTime = _effectTime * FlickerSpeed;
		bool changeColor = (GD.Randf() < FlickerChance) && (Mathf.PosMod(noiseTime, 1.0f) < FlickerDuration);
		Modulate = changeColor ? GlitchColor : baseColor;

		// Erratic scale using stepped noise
		float baseScale = MinScale;
		if (GD.Randf() < JumpChance) // Random chance for sudden scale change
		{
			baseScale = Mathf.Lerp(MinScale, MaxScale, GD.Randf());
		}
		// Add high-frequency small jitter
		float jitter = (GD.Randf() * JitterAmount * 2.0f) - JitterAmount;
		float scale = baseScale + jitter;
		Scale = new Vector2(scale, scale);
	}
}
