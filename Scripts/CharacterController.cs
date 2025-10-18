using Godot;

public partial class CharacterController : CharacterBody2D
{
	// Movement parameters
	[Export] public float MaxSpeed = 300.0f;
	[Export] public float Acceleration = 1500.0f;
	[Export] public float Friction = 1200.0f;
	[Export] public float AirAcceleration = 800.0f;

	// Jump parameters
	[Export] public float JumpVelocity = -400.0f;
	[Export] public float JumpCutMultiplier = 0.5f;
	[Export] public float CoyoteTime = 0.1f;
	[Export] public float JumpBufferTime = 0.1f;

	// Gravity parameters
	[Export] public float Gravity = 980.0f;
	[Export] public float FallMultiplier = 1.5f;
	[Export] public float MaxFallSpeed = 600.0f;

	// Input controller
	[Export] public NodePath InputControllerPath;
	[Export] public PackedScene ExplosionScene;

	private float _coyoteTimeCounter;
	private float _jumpBufferCounter;
	private bool _wasJumpPressed;
	private AnimatedSprite2D _sprite;
	private Sprite2D _dropShadow;
	private InputController _inputController;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_dropShadow = GetNode<Sprite2D>("DropShadow");

		// Get input controller reference
		if (InputControllerPath != null)
		{
			_inputController = GetNode<InputController>(InputControllerPath);
			if (_inputController == null)
			{
				GD.PushWarning($"[CharacterController] InputController not found at path: {InputControllerPath}");
			}
		}
		else
		{
			GD.PushWarning("[CharacterController] InputControllerPath is not set. Character will not respond to input.");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		float deltaF = (float)delta;

		// Handle action button
		if (_inputController != null && _inputController.IsActionJustPressed())
		{
			OnActionPressed();
		}

		// Get input direction from InputController
		float direction = 0;
		if (_inputController != null)
		{
			direction = _inputController.GetMoveDirection();
		}

		// Coyote time - allows jumping shortly after leaving platform
		if (IsOnFloor())
		{
			_coyoteTimeCounter = CoyoteTime;
		}
		else
		{
			_coyoteTimeCounter -= deltaF;
		}

		// Jump buffer - remembers jump input slightly before landing
		bool isJumpPressed = false;
		if (_inputController != null)
		{
			isJumpPressed = _inputController.IsJumpPressed();
			if (isJumpPressed && !_wasJumpPressed)
			{
				_jumpBufferCounter = JumpBufferTime;
			}
			else
			{
				_jumpBufferCounter -= deltaF;
			}
		}

		// Handle jump
		if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0)
		{
			velocity.Y = JumpVelocity;
			_jumpBufferCounter = 0;
			_coyoteTimeCounter = 0;
		}

		// Variable jump height - release jump for shorter jump
		if (_wasJumpPressed && !isJumpPressed && velocity.Y < 0)
		{
			velocity.Y *= JumpCutMultiplier;
		}

		_wasJumpPressed = isJumpPressed;

		// Apply gravity with fall multiplier for snappier falls
		if (!IsOnFloor())
		{
			if (velocity.Y > 0)
			{
				velocity.Y += Gravity * FallMultiplier * deltaF;
			}
			else
			{
				velocity.Y += Gravity * deltaF;
			}

			// Cap fall speed
			velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
		}

		// Horizontal movement with acceleration
		if (direction != 0)
		{
			float accel = IsOnFloor() ? Acceleration : AirAcceleration;
			velocity.X = Mathf.MoveToward(velocity.X, direction * MaxSpeed, accel * deltaF);

			// Flip sprite to face movement direction
			if (_sprite != null)
			{
				_sprite.FlipH = direction > 0;
			}
		}
		else
		{
			// Apply friction
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * deltaF);
		}

		// Update animation based on state
		if (_sprite != null)
		{
			if (!IsOnFloor())
			{
				_sprite.Play("air");
			}
			else if (Mathf.Abs(velocity.X) > 10)
			{
				_sprite.Play("run");
			}
			else
			{
				_sprite.Play("idle");
			}
		}

		Velocity = velocity;
		MoveAndSlide();

		// Update drop shadow position AFTER movement
		if (_dropShadow != null)
		{
			var spaceState = GetWorld2D().DirectSpaceState;
			var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + Vector2.Down * 1000);
			query.CollideWithAreas = false;
			query.CollideWithBodies = true;
			query.Exclude = new Godot.Collections.Array<Rid> { GetRid() }; // Exclude character itself

			var result = spaceState.IntersectRay(query);
			if (result.Count > 0)
			{
				Vector2 hitPosition = (Vector2)result["position"];
				_dropShadow.GlobalPosition = hitPosition;
				_dropShadow.Visible = true;
			}
			else
			{
				_dropShadow.Visible = false;
			}
		}
	}

	private void OnActionPressed()
	{
		if (ExplosionScene != null)
		{
			// Instance explosion at player position
			var explosion = ExplosionScene.Instantiate();
			if (explosion is Node2D explosion2D)
			{
				explosion2D.GlobalPosition = GlobalPosition;
				GetParent().AddChild(explosion);
			}

			// Remove player from game
			QueueFree();
		}
		else
		{
			GD.PushWarning("[CharacterController] ExplosionScene is not set!");
		}
	}
}
