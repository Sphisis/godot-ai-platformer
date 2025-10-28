using System;
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

	// Attack combo system
	[Export] public float ComboWindow = 0.5f; // Time window to continue combo
	[Export] public float AttackImpulse = 150.0f; // Forward impulse during attacks

	private float _coyoteTimeCounter;
	private float _jumpBufferCounter;
	private bool _wasJumpPressed;
	private AnimatedSprite2D _sprite;
	private Sprite2D _dropShadow;
	private InputController _inputController;

	// Attack combo state
	private int _currentComboStep = 1; // 0 = no combo, 1-3 = combo steps
	private float _comboTimer = 0f;
	private bool _isAttacking = false;

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
		float deltaF = (float)delta;
		Vector2 velocity = Velocity;

		// Core gameplay systems
		HandleAttack(ref velocity);
		UpdateAttack(deltaF);
		HandleJump(ref velocity, deltaF);
		HandleGravity(ref velocity, deltaF);
		HandleHorizontalMovement(ref velocity, deltaF);
		
		// Visual updates
		UpdateAnimation(velocity);
		
		// Apply movement
		Velocity = velocity;
		MoveAndSlide();
		
		// Post-movement updates
		UpdateDropShadow();
	}

	private void HandleAttack(ref Vector2 velocity)
	{
		if (_isAttacking) return;

		if (!_inputController.IsActionJustPressed()) return;
		
		// reset combo
		if (_comboTimer <= 0f)
		{
			_currentComboStep = 1;
		}

		// Play appropriate attack animation
		_sprite.SpeedScale = 1f;
		string attackAnim = $"attack{_currentComboStep}";
		_sprite.Play(attackAnim);

		_isAttacking = true;
		_comboTimer = ComboWindow;

		// Add velocity impulse to attack direction
		float attackDirection = _sprite.FlipH ? -1.0f : 1.0f; // Use sprite facing direction
		velocity.X = attackDirection * AttackImpulse;
	}

	private void UpdateAttack(float deltaF)
	{
		if (_comboTimer > 0f) _comboTimer -= deltaF;

		if (!_isAttacking) return;
		
		// Check if current attack animation finished
		if (_sprite.IsPlaying()) return;

		_isAttacking = false;
		_currentComboStep++;
		if (_currentComboStep > 3) _currentComboStep = 1;
	}

	private void HandleJump(ref Vector2 velocity, float deltaF)
	{
		if (_isAttacking) return;

		// Update coyote time
		if (IsOnFloor())
		{
			_coyoteTimeCounter = CoyoteTime;
		}
		else
		{
			_coyoteTimeCounter -= deltaF;
		}

		// Update jump buffer
		bool isJumpPressed = _inputController?.IsJumpPressed() ?? false;
		if (isJumpPressed && !_wasJumpPressed)
		{
			_jumpBufferCounter = JumpBufferTime;
		}
		else
		{
			_jumpBufferCounter -= deltaF;
		}

		// Execute jump
		if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0)
		{
			velocity.Y = JumpVelocity;
			_jumpBufferCounter = 0;
			_coyoteTimeCounter = 0;
		}

		// Variable jump height
		if (_wasJumpPressed && !isJumpPressed && velocity.Y < 0)
		{
			velocity.Y *= JumpCutMultiplier;
		}

		_wasJumpPressed = isJumpPressed;
	}

	private void HandleGravity(ref Vector2 velocity, float deltaF)
	{
		if (!IsOnFloor())
		{
			float gravityToApply = velocity.Y > 0 ? Gravity * FallMultiplier : Gravity;
			velocity.Y += gravityToApply * deltaF;
			velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
		}
	}

	private void HandleHorizontalMovement(ref Vector2 velocity, float deltaF)
	{
		float direction = _inputController?.GetMoveVector().X ?? 0;

		if (!_isAttacking && Mathf.Abs(direction) > 0.001f)
		{
			// Apply acceleration
			float accel = IsOnFloor() ? Acceleration : AirAcceleration;
			velocity.X = Mathf.MoveToward(velocity.X, direction * MaxSpeed, accel * deltaF);
			
			// Update sprite direction
			_sprite.FlipH = direction < 0;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * deltaF);
		}
	}

	private void UpdateAnimation(Vector2 velocity)
	{
		// Don't change animation if currently attacking
		if (_isAttacking) return;

		if (!IsOnFloor())
		{
			_sprite.SpeedScale = 1f;
			_sprite.Play("air");
		}
		else if (Mathf.Abs(velocity.X) > 10)
		{
			_sprite.Play("run");
			
			// Scale animation speed based on movement speed (4-12 fps range)
			float speedRatio = Mathf.Abs(velocity.X) / MaxSpeed;
			float animSpeed = Mathf.Lerp(4.0f, 12.0f, speedRatio);
			_sprite.SpeedScale = animSpeed / (float)_sprite.SpriteFrames.GetAnimationSpeed("run");
		}
		else
		{
			_sprite.SpeedScale = 1f;
			_sprite.Play("idle");
		}
	}

	private void UpdateDropShadow()
	{
		if (_dropShadow == null) return;

		var spaceState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + Vector2.Down * 1000);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

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

	private void OnActionPressed()
	{
		// Legacy explosion behavior - kept for compatibility
		// Remove this method if no longer needed
		if (ExplosionScene != null)
		{
			var explosion = ExplosionScene.Instantiate();
			if (explosion is Node2D explosion2D)
			{
				explosion2D.GlobalPosition = GlobalPosition;
				GetParent().AddChild(explosion);
			}
			QueueFree();
		}
	}
}
