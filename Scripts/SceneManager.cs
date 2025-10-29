using Godot;
using System;

public partial class SceneManager : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Quick quit with Escape key
		if (Input.IsActionJustPressed("ui_cancel") || Input.IsKeyPressed(Key.Escape))
		{
			QuitGame();
		}
	}

	private void QuitGame()
	{
		// Clean exit
		GetTree().Quit();
	}
}
