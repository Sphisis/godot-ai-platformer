using Godot;
using System;

public partial class Collectible : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
    	// Connect the body entered signal
		BodyEntered += OnBodyEntered;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("Player")) return;
		QueueFree();
	}
}
