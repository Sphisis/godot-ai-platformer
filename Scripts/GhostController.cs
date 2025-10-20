using Godot;
using System;

public partial class GhostController : CharacterBody2D
{
	// Movement tuning
	[Export] public float Speed = 120f;
	[Export] public float SpeedAdditionPerSecond = 10f;
	[Export] public float Acceleration = 400f;

	// Optional wiring
	[Export] public NodePath TargetPath;
	[Export] public NodePath AgentPath;
	[Export] public NodePath CameraPath; // optional Camera2D to check visibility from
	[Export] public bool DebugLos = false;
	[Export] public float LosTolerance = 8.0f;

	private CharacterBody2D _target;
	private AnimatedSprite2D _sprite;
	private NavigationAgent2D _agent;
	private Vector2 _lastRequestedTarget = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
	private bool targetVisible;

	// Debug draw
	private Vector2 _debugRayFrom = Vector2.Zero;
	private Vector2 _debugHitPos = Vector2.Zero;
	private bool _debugHasHit = false;
	private bool _debugSees = false;

	// Sprite effect properties
	private float _effectTime = 0f;

	[ExportGroup("Glitch Effect")]
	[Export(PropertyHint.Range, "0,50,1")] 
	public float FlickerSpeed = 30f;  // How fast the color changes

	[Export(PropertyHint.Range, "0,1,0.1")] 
	public float FlickerChance = 0.5f;  // Chance to change color each frame

	[Export(PropertyHint.Range, "0,1,0.1")] 
	public float FlickerDuration = 0.7f;  // How long each color stays (0-1)

	[Export] 
	public Color GlitchColor = new Color(1, 0, 0, 0.8f);  // Color to flicker to

	[ExportGroup("Scale Effect")]
	[Export(PropertyHint.Range, "0.5,2.0,0.1")] 
	public float MinScale = 1.0f;

	[Export(PropertyHint.Range, "0.5,2.0,0.1")] 
	public float MaxScale = 1.4f;

	[Export(PropertyHint.Range, "0,0.5,0.01")] 
	public float JitterAmount = 0.05f;  // How much random scale jitter to add

	[Export(PropertyHint.Range, "0,1,0.1")] 
	public float JumpChance = 0.5f;  // Chance for sudden scale changes

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<AnimatedSprite2D>("ghost") ?? GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite");
		_target = ResolveTarget();
		_agent = ResolveAgent();

		if (_agent != null)
		{
			_agent.PathDesiredDistance = 6f;
			_agent.TargetDesiredDistance = 6f;
			_agent.MaxSpeed = Speed;
		}
		else
		{
			GD.PushWarning("[GhostController] NavigationAgent2D not found. Ghost will use simple pursuit.");
		}
	}

	// Find target from explicit path or scene search
	private CharacterBody2D ResolveTarget()
	{
		if (TargetPath != null && !string.IsNullOrEmpty(TargetPath.ToString()))
			return GetNodeOrNull<CharacterBody2D>(TargetPath);

		var parent = GetParent();
		if (parent != null)
		{
			var p = parent.GetNodeOrNull<CharacterBody2D>("Pacman");
			if (p != null) return p;
		}

		var root = GetTree().Root;
		var found = NodeUtils.FindNodeByName(root, "Pacman");
		return found as CharacterBody2D;
	}

	// Locate NavigationAgent2D child by exported path, name, or type
	private NavigationAgent2D ResolveAgent()
	{
		if (AgentPath != null && !string.IsNullOrEmpty(AgentPath.ToString()))
			return GetNodeOrNull<NavigationAgent2D>(AgentPath);

		var named = GetNodeOrNull<NavigationAgent2D>("NavigationAgent2D");
		if (named != null) return named;

		foreach (var child in GetChildren())
			if (child is NavigationAgent2D a) return a;

		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_target == null) return;
		float dt = (float)delta;

		if (_agent != null)
			UpdateWithAgent(dt);
		else
			UpdateDirectPursuit(dt);

		UpdateSprite();

		// Line-of-sight visibility using the reusable utility (ignore Areas)
		bool sees = PhysicsUtils.IsLineOfSightBetween(this, _target, LosTolerance, collideWithAreas: false);
		if (sees != targetVisible)
		{
			GD.Print($"[Ghost] Target visible (sees={sees})");
			targetVisible = sees;
			Visible = targetVisible;
		}
		_debugSees = sees;

		if (DebugLos)
		{
			_debugRayFrom = GlobalPosition;
			var rr = PhysicsUtils.Raycast(this, _target, collideWithAreas: false, collideWithBodies: true);
			_debugHasHit = (rr != null && rr.Count > 0 && rr.ContainsKey("position"));
			_debugHitPos = _debugHasHit ? (Vector2)rr["position"] : _target.GlobalPosition;
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (!DebugLos) return;
		var col = _debugSees ? new Color(0, 1, 0, 0.9f) : new Color(1, 0, 0, 0.9f);
		// Convert global positions to this node's local coordinates for drawing
		Vector2 localFrom = ToLocal(_debugRayFrom);
		Vector2 localHit = ToLocal(_debugHitPos);
		DrawLine(localFrom, localHit, col, 2.0f);
		DrawCircle(localHit, 4.0f, col);
	}

	private void UpdateWithAgent(float dt)
	{
		if (_agent.MaxSpeed < 42f)
		{
			Speed += SpeedAdditionPerSecond * dt;
			_agent.MaxSpeed = Speed;
		}

		const float requestThreshold = 8f;
		if ((_target.GlobalPosition - _lastRequestedTarget).Length() > requestThreshold)
		{
			_agent.TargetPosition = _target.GlobalPosition;
			_lastRequestedTarget = _target.GlobalPosition;
		}

		Vector2 next = _agent.GetNextPathPosition();

		// Runtime smoothing (workaround for navmesh clearance)
		var path = _agent.GetCurrentNavigationPath();
		int idx = _agent.GetCurrentNavigationPathIndex();
		if (path != null && idx >= 0 && idx + 1 < path.Length)
			next = (next + path[idx + 1]) * 0.5f;

		if (_agent.IsNavigationFinished() && _agent.IsTargetReached())
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 toNext = next - GlobalPosition;
		if (toNext.LengthSquared() < 1f)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 desired = toNext.Normalized();
		float useSpeed = MathF.Min(Speed, _agent.MaxSpeed);
		Vector2 desiredVel = desired * useSpeed;

		_agent.Velocity = desiredVel;
		Velocity = Velocity.MoveToward(desiredVel, Acceleration * dt);
		MoveAndSlide();
	}

	private void UpdateDirectPursuit(float dt)
	{
		Vector2 toTarget = _target.GlobalPosition - GlobalPosition;
		if (toTarget.LengthSquared() < 1f) return;
		Vector2 desired = toTarget.Normalized();
		Vector2 desiredVel = desired * Speed;
		Velocity = Velocity.MoveToward(desiredVel, Acceleration * dt);
		MoveAndSlide();
	}

	private void UpdateSprite()
	{
		if (_sprite == null) return;
		if (Velocity.Length() > 0.1f) _sprite.Play("default");
		_sprite.FlipH = Velocity.X > 0;

		// Update the effect time
		_effectTime += (float)GetProcessDeltaTime();

		// Glitchy color using noise and random
		float noiseTime = _effectTime * FlickerSpeed;
		bool changeColor = (GD.Randf() < FlickerChance) && (Mathf.PosMod(noiseTime, 1.0f) < FlickerDuration);
		_sprite.Modulate = changeColor ? GlitchColor : Colors.White;

		// Erratic scale using stepped noise
		float baseScale = MinScale;
		if (GD.Randf() < JumpChance) // Random chance for sudden scale change
		{
			baseScale = Mathf.Lerp(MinScale, MaxScale, GD.Randf());
		}
		// Add high-frequency small jitter
		float jitter = (GD.Randf() * JitterAmount * 2.0f) - JitterAmount;
		float scale = baseScale + jitter;
		_sprite.Scale = new Vector2(scale, scale);
	}
}
