using Godot;
using System;

public partial class GhostController : CharacterBody2D
{
	[Export] public float StartSpeed = 24f;
	[Export] public float MaxSpeed = 120f;
	[Export] public float TimeToMaxSpeed = 10f;
	[Export] public float Acceleration = 400f;
	[Export] public NavigationAgent2D NavAgent;
	[Export] public AnimatedSprite2D _sprite;
	[Export] public PackedScene JumpscareScene;  // Reference to jumpscare ghost scene
	
	private float LosTolerance = 8.0f;
	private CharacterBody2D _target;
	private bool isPaused;
	private float _elapsedTime = 0f;
	private GameManager _gameManager;

	private Vector2 _lastRequestedTarget = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
	private bool targetVisible;
	private Vector2 _startPosition;
	private bool _isWaiting = false;
	private float _waitTimer = 0f;

	public override void _Ready()
	{
		// Store starting position
		_startPosition = GlobalPosition;

		NavAgent.MaxSpeed = StartSpeed;
		_target = ResolveTarget();

		// Get GameManager singleton
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_gameManager.ResetLevel += OnResetLevel;
		_gameManager.PlayerVictory += OnVictory;
	}
	
	private void OnVictory()
    {
		isPaused = true;
    }   

	public void OnResetLevel()
	{
		// Reset position
		GlobalPosition = _startPosition;
		
		// Reset velocity
		Velocity = Vector2.Zero;
		
		// Reset navigation
		NavAgent.MaxSpeed = StartSpeed;
		_lastRequestedTarget = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		
		// Reset state
		isPaused = false;
		_elapsedTime = 0f;
		targetVisible = false;
		_isWaiting = false;
		_waitTimer = 0f;
		
		// Reset visibility
		Visible = false;
		if (_sprite != null)
		{
			_sprite.Visible = true;
		}
		
		GD.Print("[GhostController] Reset to start position");
	}

	private CharacterBody2D ResolveTarget()
	{
		var playerNodes = GetTree().GetNodesInGroup("Player");
		if (playerNodes.Count > 0)
		{
			return playerNodes[0] as CharacterBody2D;
		}
		throw new Exception("Could not find player from the scene!");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isPaused) return;

		if (_target == null) return;
		float dt = (float)delta;

		// Handle waiting state
		if (_isWaiting)
		{
			_waitTimer -= dt;
			if (_waitTimer <= 0f)
			{
				_isWaiting = false;
			}
			else
			{
				// Stay still while waiting
				Velocity = Vector2.Zero;
				MoveAndSlide();
				return;
			}
		}

		// move
		UpdateWithAgent(dt);

		// face movement direction
		_sprite.FlipH = Velocity.X > 0;

		// Check for collision with target
		CheckTargetCollision();

		// Line-of-sight visibility using the reusable utility (ignore Areas)
		bool sees = PhysicsUtils.IsLineOfSightBetween(this, _target, LosTolerance, collideWithAreas: false);
		if (sees != targetVisible)
		{
			targetVisible = sees;
			Visible = targetVisible;
			if (!targetVisible)
			{
				// 50% chance to wait 2-5 seconds when player goes out of sight
				if (GD.Randf() < 0.5f)
				{
					_isWaiting = true;
					_waitTimer = 2.0f + GD.Randf() * 3.0f;  // Random between 2-5 seconds
					GD.Print($"[GhostController] Lost sight of player, waiting {_waitTimer:F1}s");
				}
			}
		}
	}

	private void UpdateWithAgent(float dt)
	{
		// slowly pick up speed to maximum speed (inverse function: fast at start, slows down approaching max)
		_elapsedTime += dt;
		float t = 1f - Mathf.Exp(-_elapsedTime / TimeToMaxSpeed * 5f);  // Exponential approach to 1
		NavAgent.MaxSpeed = Mathf.Lerp(StartSpeed, MaxSpeed, t);

		// assign new target to navAgent
		const float requestThreshold = 8f;
		if ((_target.GlobalPosition - _lastRequestedTarget).Length() > requestThreshold)
		{
			NavAgent.TargetPosition = _target.GlobalPosition;
			_lastRequestedTarget = _target.GlobalPosition;
		}

		// Agent is at the target position
		if (NavAgent.IsNavigationFinished() && NavAgent.IsTargetReached())
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Get direction to new waypoint
		Vector2 toNext = NavAgent.GetNextPathPosition() - GlobalPosition;

		// Skip waypoints that are too close (agent overshooting)
		if (toNext.LengthSquared() < 1f && !NavAgent.IsTargetReached())
		{
			// Continue to next waypoint instead of stopping
			toNext = NavAgent.GetNextPathPosition() - GlobalPosition;
		}

		NavAgent.Velocity = toNext.Normalized() * NavAgent.MaxSpeed;
		Velocity = Velocity.MoveToward(NavAgent.Velocity, Acceleration * dt);
		MoveAndSlide();
	}
	
	private void CheckTargetCollision()
	{
		// Check if we're colliding with the target
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var collider = collision.GetCollider();
			
			if (collider == _target)
			{
				isPaused = true;
				_sprite.Visible = false;
				SpawnJumpscare();
				
				// Trigger death through GameManager
				if (_gameManager != null)
				{
					_gameManager.TriggerPlayerDeath();
				}
				else
				{
					GD.PushWarning("[GhostController] GameManager not found");
				}
				
				return;
			}
		}
	}
	
	private void SpawnJumpscare()
	{
		if (JumpscareScene == null)
		{
			GD.PushWarning("[GhostController] JumpscareScene not set");
			return;
		}
		
		var jumpscare = JumpscareScene.Instantiate();
		if (jumpscare is JumpscareGhost js)
		{
			// Add to scene at ghost's position
			GetParent().AddChild(js);
			js.GlobalPosition = GlobalPosition;
			js.Trigger();
		}
		else
		{
			GD.PushWarning("[GhostController] JumpscareScene is not a JumpscareGhost");
		}
	}
}
