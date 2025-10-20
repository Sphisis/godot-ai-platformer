using Godot;
using System;

// Simple movement controller that reads InputController.GetMoveVector() and moves/collides via MoveAndSlide
public partial class packmanController : CharacterBody2D
{
	[Export] public NodePath InputControllerPath;
	[Export] public float MaxSpeed = 200f;
	[Export] public float Acceleration = 800f;

	private InputController _inputController;
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (InputControllerPath != null)
		{
			_inputController = GetNodeOrNull<InputController>(InputControllerPath);
			if (_inputController == null)
				GD.PushWarning($"[packmanController] InputController not found at path: {InputControllerPath}");
		}
		else
		{
			GD.PushWarning("[packmanController] InputControllerPath is not set. Controller will not respond to input.");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		float dt = (float)delta;

		Vector2 move = Vector2.Zero;
		if (_inputController != null)
		{
			move = _inputController.GetMoveVector();
		}

		// Smooth acceleration toward desired velocity
		Vector2 target = move * MaxSpeed;
		velocity.X = Mathf.MoveToward(velocity.X, target.X, Acceleration * dt);
		velocity.Y = Mathf.MoveToward(velocity.Y, target.Y, Acceleration * dt);

		Velocity = velocity;
		MoveAndSlide();

		// Simple animation state: play idle or run
		if (_sprite != null)
		{
			if (move.Length() > 0.1f)
			{
				_sprite.Play("run");
				_sprite.FlipH = move.X > 0; // flip depending on horizontal direction
			}
			else
			{
				_sprite.Play("idle");
			}
		}
	}
}
