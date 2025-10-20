using Godot;
using System;

public partial class ThingCounter : Label
{
	private int _thingsCollected = 0;
	
	public override void _Ready()
	{
		// Initialize the label
		UpdateLabel();
		
		// Connect to all collectibles in the scene
		CallDeferred(nameof(ConnectToCollectibles));
	}

	private void ConnectToCollectibles()
	{
		// Find all nodes in the Collectable group and connect to their signals
		var collectibles = GetTree().GetNodesInGroup("Collectable");
		foreach (Node node in collectibles)
		{
			if (node is Collectable collectible)
			{
				collectible.Collected += OnCollectibleCollected;
			}
		}
	}

	private void OnCollectibleCollected()
	{
		_thingsCollected++;
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		Text = $"THINGS: {_thingsCollected}";
	}
}
