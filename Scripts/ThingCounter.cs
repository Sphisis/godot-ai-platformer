using Godot;
using System;

public partial class ThingCounter : Label
{
	private int _thingsCollected = 100;
	private Tween _scaleTween;

	[Export]
	public float PopScale = 1.3f;  // How much to scale up on collect

	[Export]
	public float PopDuration = 0.15f;  // Duration of the pop animation

	public override void _Ready()
	{
		// Initialize the label
		UpdateLabel();

		// Connect to all collectibles in the scene
		CallDeferred(nameof(ConnectToCollectibles));

		GetNode<GameManager>("/root/GameManager").ResetLevel += OnResetLevel;
	}

	private void OnResetLevel()
	{
		_thingsCollected = 100;
		UpdateLabel();
		GD.Print("[ThingCounter] Reset complete");
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
		_thingsCollected--;
		UpdateLabel();
		PlayPopAnimation();
	}
	
	private void PlayPopAnimation()
	{
		// Kill any existing tween
		if (_scaleTween != null && _scaleTween.IsRunning())
		{
			_scaleTween.Kill();
		}
		
		// Create a new tween for the pop effect
		_scaleTween = CreateTween();
		_scaleTween.SetEase(Tween.EaseType.Out);
		_scaleTween.SetTrans(Tween.TransitionType.Back);
		
		// Scale up quickly
		_scaleTween.TweenProperty(this, "scale", Vector2.One * PopScale, PopDuration * 0.4f);
		
		// Scale back to normal
		_scaleTween.TweenProperty(this, "scale", Vector2.One, PopDuration * 0.6f);
	}

	private void UpdateLabel()
	{
		Text = $"{_thingsCollected} LEFT!";
	}
}
