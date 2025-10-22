using Godot;
using System;

public partial class GameManager : Node
{
	[Signal]public delegate void PlayerDeathEventHandler();
	[Signal] public delegate void ResetLevelEventHandler();
    [Signal] public delegate void PlayerVictoryEventHandler();
    [Signal] public delegate void PauseEventHandler(bool isPaused);
    
	private float DeathDelaySeconds = 2.5f;  // Time to wait before resetting after death
	private float VictoryDelaySeconds = 3.0f;  // Time to wait before loading next scene
	private string NextScenePath = "res://Scenes/Test.tscn";  // Path to the next scene to load after victory
	
	private bool _isResetting = false;

	public void TriggerPlayerDeath()
	{
		if (_isResetting) return;
		EmitSignal(SignalName.PlayerDeath);
		_isResetting = true;
		GetTree().CreateTimer(DeathDelaySeconds).Timeout += OnResetTimer;
	}

	private void OnResetTimer()
	{
		EmitSignal(SignalName.ResetLevel);
		_isResetting = false;
	}
	
	public void TriggerPlayerVictory()
	{
		EmitSignal(SignalName.PlayerVictory);
		
		// Wait for victory animation, then load next scene
		GetTree().CreateTimer(VictoryDelaySeconds).Timeout += OnVictoryTimer;
	}
	
	private void OnVictoryTimer()
	{
		if (!string.IsNullOrEmpty(NextScenePath))
		{
			GD.Print($"[GameManager] Loading next scene: {NextScenePath}");
			GetTree().ChangeSceneToFile(NextScenePath);
		}
		else
		{
			GD.PushWarning("[GameManager] NextScenePath is not set - cannot load next scene");
		}
	}

    public override void _Ready()
    {
        GD.Print("[GameManager] Initialized");
    }
    
    public void SetPause(bool isPaused)
    {
        EmitSignal(SignalName.Pause, isPaused);
    }
}
