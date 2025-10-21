using Godot;
using System;

public partial class GameManager : Node
{
	[Signal]
	public delegate void PlayerDeathEventHandler();
	
	[Signal]
	public delegate void ResetLevelEventHandler();
	
	[Signal]
	public delegate void PlayerVictoryEventHandler();
	
	[Export]
	public float DeathDelaySeconds = 2.5f;  // Time to wait before resetting after death
	
	private bool _isResetting = false;

	public void TriggerPlayerDeath()
	{
		if (_isResetting) return;
		
		GD.Print("[GameManager] Player death triggered");
		EmitSignal(SignalName.PlayerDeath);
		
		_isResetting = true;
		
		// Wait for death animation/jumpscare, then reset
		GetTree().CreateTimer(DeathDelaySeconds).Timeout += OnResetTimer;
	}

	private void OnResetTimer()
	{
		GD.Print("[GameManager] Resetting level...");
		EmitSignal(SignalName.ResetLevel);
		_isResetting = false;
	}
	
	public void TriggerPlayerVictory()
	{
		GD.Print("[GameManager] Player victory!");
		EmitSignal(SignalName.PlayerVictory);
	}
	
	public override void _Ready()
	{
		GD.Print("[GameManager] Initialized");
	}
}
