using Godot;
using System;

public partial class JumpscareGhost : AnimatedSprite2D
{
	[Export]
	public float ScaleSpeed = 15f;  // How fast it scales up
	
	[Export]
	public float TargetScale = 50f;  // Final scale to cover screen
	
	[Export]
	public float GlitchSpeed = 60f;  // How fast the glitch effect
	
	private float _currentScale = 0.1f;
	private float _glitchTime = 0f;
	private bool _isActive = false;

	public override void _Ready()
	{
		Scale = Vector2.Zero;
		Modulate = new Color(1, 1, 1, 0);
	}

	public void Trigger()
	{
		_isActive = true;
		Play("default");
	}

	public override void _Process(double delta)
	{
		if (!_isActive) return;
		
		float dt = (float)delta;
		
		// Scale up rapidly
		_currentScale += ScaleSpeed * dt;
		Scale = Vector2.One * _currentScale;
		
		// Fade in
		float alpha = Mathf.Clamp(_currentScale / 2f, 0f, 1f);
		
		// Glitch effect
		_glitchTime += dt * GlitchSpeed;
		bool glitch = (GD.Randf() < 0.7f) && (Mathf.Sin(_glitchTime) > 0.3f);
		
		Color glitchColor = glitch ? new Color(GD.Randf(), GD.Randf(), GD.Randf(), alpha) : new Color(1, 1, 1, alpha);
		Modulate = glitchColor;
		
		// Random offset for extra chaos
		if (GD.Randf() < 0.3f)
		{
			Offset = new Vector2(
				(GD.Randf() - 0.5f) * 20f,
				(GD.Randf() - 0.5f) * 20f
			);
		}
		
		// Stop when reaching target scale
		if (_currentScale >= TargetScale)
		{
			_isActive = false;
			QueueFree();
		}
	}
}
