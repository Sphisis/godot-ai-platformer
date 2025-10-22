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
	private bool isDead;
	private bool isPaused;
	private float _deadTime = 0f;
	private float _jumpVelocity = -150f;
	private float _gravity = 980f;
	private GameManager _gameManager;
	private Vector2 _startPosition;

	public override void _Ready()
	{
		// Store starting position
		_startPosition = GlobalPosition;

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

		// Connect to GameManager
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_gameManager.PlayerDeath += OnPlayerDeath;
		_gameManager.ResetLevel += OnResetLevel;
		_gameManager.PlayerVictory += () => isPaused = true;
		_gameManager.Pause += (bool state) => isPaused = state;

		CallDeferred(nameof(Start));
	}

	private void Start()
	{
		// pause game immediately after start
		_gameManager.SetPause(true);

		// wait for few seconds and then give player the chance to start the game with any key
		var timer = new Timer();
		timer.WaitTime = 2.0f;
		timer.OneShot = true;
		AddChild(timer);
		timer.Timeout += () =>
		{
			timer.QueueFree();
			_inputController.AnyKeyPressed += StartGame;
		};
		timer.Start();
	}

	private void StartGame()
	{
		GD.Print("Any key pressed!");
		_inputController.AnyKeyPressed -= StartGame;
		_gameManager.SetPause(false);
	}

	private void OnResetLevel()
	{
		// Reset position
		GlobalPosition = _startPosition;
		Velocity = Vector2.Zero;

		// Reset state
		isDead = false;
		_deadTime = 0f;

		// Re-enable collision
		SetCollisionLayerValue(1, true);
		SetCollisionMaskValue(1, true);

		// Reset animation
		if (_sprite != null)
		{
			_sprite.Play("idle");
		}
	}
	
	private void OnPlayerDeath()
	{
		isDead = true;
		// Disable collision
		SetCollisionLayerValue(1, false);
		SetCollisionMaskValue(1, false);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isPaused) return;

		if (isDead)
        {
			UpdateDead(delta);
			return;
        }
		UpdateGame(delta);
    }

	private void UpdateGame(double delta)
	{
		Vector2 velocity = Velocity;
		float dt = (float)delta;

		Vector2 move = Vector2.Zero;
		move = _inputController.GetMoveVector();

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
	
	private void UpdateDead(double delta)
	{
		float dt = (float)delta;
		_deadTime += dt;
		
		// Apply jump velocity at start, then apply gravity
		if (_deadTime < 0.1f)
		{
			Velocity = new Vector2(0, _jumpVelocity);
		}
		else
		{
			Velocity = new Vector2(0, Velocity.Y + _gravity * dt);
		}
		
		MoveAndSlide();
		
		// Play death animation if available
		if (_sprite != null)
		{
			_sprite.Play("idle");  // Or "death" if you have that animation
		}
	}
}
