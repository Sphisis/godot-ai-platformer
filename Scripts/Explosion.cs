using Godot;

public partial class Explosion : AnimatedSprite2D
{
	public override void _Ready()
	{
		// Connect to the animation finished signal
		AnimationFinished += OnAnimationFinished;
	}

	private void OnAnimationFinished()
	{
		Stop();
		
		// Clean up the explosion object after animation completes
		QueueFree();
	}
}
