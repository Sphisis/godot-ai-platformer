using Godot;

public partial class CameraFollow : Camera2D
{
	[Export] public NodePath TargetPath;
	[Export] public float LookAheadDistance = 100.0f;
	[Export] public float SmoothSpeed = 5.0f;
	[Export] public float VerticalOffset = -50.0f;
	[Export] public float LookAheadSmoothSpeed = 3.0f;

	private Node2D _target;
	private float _currentLookAhead;
	private float _targetLookAhead;

	public override void _Ready()
	{
		// Guard: TargetPath may be unset or empty in some scenes. Avoid calling GetNode with an empty path.
		var pathStr = TargetPath?.ToString();
		if (!string.IsNullOrEmpty(pathStr))
		{
			if (HasNode(TargetPath))
			{
				_target = GetNode<Node2D>(TargetPath);
			}
			else
			{
				GD.PushWarning($"[CameraFollow] TargetPath '{TargetPath}' not found. Camera will not follow.");
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_target == null) return;

		float deltaF = (float)delta;

		// Determine look-ahead direction based on target velocity
		if (_target is CharacterBody2D character)
		{
			if (Mathf.Abs(character.Velocity.X) > 10)
			{
				_targetLookAhead = Mathf.Sign(character.Velocity.X) * LookAheadDistance;
			}
			else
			{
				_targetLookAhead = 0;
			}
		}

		// Smoothly interpolate look-ahead
		_currentLookAhead = Mathf.Lerp(_currentLookAhead, _targetLookAhead, LookAheadSmoothSpeed * deltaF);

		// Calculate target position with look-ahead and vertical offset
		Vector2 targetPosition = new Vector2(
			_target.GlobalPosition.X + _currentLookAhead,
			_target.GlobalPosition.Y + VerticalOffset
		);

		// Smoothly move camera to target position
		GlobalPosition = GlobalPosition.Lerp(targetPosition, SmoothSpeed * deltaF);
	}
}
