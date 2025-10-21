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

	public override void _Ready()
	{
		// Add to Collectable group for physics exclusion
		AddToGroup("Collectable");

		// Set up respawn timer
		_respawnTimer = new Timer();
		_respawnTimer.OneShot = true;
		_respawnTimer.WaitTime = RespawnTime;
		_respawnTimer.Timeout += OnRespawnTimerTimeout;
		AddChild(_respawnTimer);

		// Get collision shape
		_collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		// Connect the body entered signal
		BodyEntered += OnBodyEntered;

		GetNode<GameManager>("/root/GameManager").ResetLevel += OnResetLevel;
	}
	
	private void OnResetLevel()
	{
		OnRespawnTimerTimeout();
	}

	private void OnBodyEntered(Node2D body)
	{
		// Check if the entering body is the player
		if (body.IsInGroup("Player"))
		{
			GD.Print($"[Collectable] Collected by player!");
			
			// Emit the collected signal
			EmitSignal(SignalName.Collected);
			
			// Hide the collectible and disable collision
			Visible = false;
			if (_collision != null) _collision.SetDeferred("disabled", true);
			
			// Start respawn timer
			_respawnTimer.Start();
		}
	}
	
	private void OnRespawnTimerTimeout()
	{
		// Show the collectible and enable collision
		Visible = true;
		if (_collision != null) _collision.SetDeferred("disabled", false);
		GD.Print($"[Collectable] Respawned!");
	}
}
