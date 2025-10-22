using Godot;
using System;

public partial class Collectable : Area2D
{
	[Signal]
	public delegate void CollectedEventHandler();
	
	[Export]
	public float RespawnTime = 3.0f;  // Time in seconds before the collectible reappears
	
	private Timer _respawnTimer;
	private CollisionShape2D _collision;
	private bool isPaused;
	private bool _isRespawning = false;
	private float _respawnProgress = 0f;

	public override void _Ready()
	{
		// Add to Collectable group for physics exclusion
		AddToGroup("Collectable");

		// Get collision shape
		_collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		// Connect the body entered signal
		BodyEntered += OnBodyEntered;

		var _gameManager = GetNode<GameManager>("/root/GameManager");

		_gameManager.ResetLevel += () =>
		{
			isPaused = false;
			Spawn();
		};
		_gameManager.PlayerVictory += () => isPaused = true;
		_gameManager.Pause += (bool state) => isPaused = state;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (isPaused) return;

		if (!body.IsInGroup("Player")) return;

		Collect();
		EmitSignal(SignalName.Collected);
	}

	public override void _Process(double delta)
	{
		if (isPaused) return;
		if (!_isRespawning) return;

		_respawnProgress += (float)delta;
		if (_respawnProgress < RespawnTime) return;

		Spawn();
	}

	private void Spawn()
	{
		Visible = true;
		_isRespawning = false;
		_collision.SetDeferred("disabled", false);
	}
	
	private void Collect()
	{
		Visible = false;
		_respawnProgress = 0f;
		_isRespawning = true;
		_collision.SetDeferred("disabled", false);
        
	}
}
