using Godot;
using System;

public partial class InputController : Node
{
	// Input events
	// MoveInput now sends a Vector2 (x = horizontal, y = vertical)
	[Signal] public delegate void MoveInputEventHandler(Vector2 direction);
	[Signal] public delegate void JumpPressedEventHandler();
	[Signal] public delegate void JumpReleasedEventHandler();
	[Signal] public delegate void ActionPressedEventHandler();
	[Signal] public delegate void ActionReleasedEventHandler();

	// Gamepad settings
	[Export] public int GamepadDevice = 0;
	[Export] public float DeadZone = 0.2f;
	[Export] public bool EnableDebugLogging = false;

	private Vector2 _previousMove = Vector2.Zero;
	private bool _wasJumpPressed;
	private bool _wasActionPressed;

	public override void _Process(double delta)
	{
	// Get horizontal and vertical input from keyboard or gamepad
	float horizontal = 0;
		float vertical = 0;

		// Keyboard input
		if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A))
			horizontal -= 1;
		if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D))
			horizontal += 1;

		// Vertical keyboard input (Up/Down or W/S)
		if (Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.W))
			vertical -= 1;
		if (Input.IsKeyPressed(Key.Down) || Input.IsKeyPressed(Key.S))
			vertical += 1;

		// Gamepad input (left stick or D-pad)
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			float stickX = Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftX);
			if (Mathf.Abs(stickX) > DeadZone)
			{
				horizontal = stickX;
			}

			// Vertical from left stick Y
			float stickY = Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftY);
			if (Mathf.Abs(stickY) > DeadZone)
			{
				vertical = stickY;
			}

			// D-pad fallback (horizontal)
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadLeft))
				horizontal = -1;
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadRight))
				horizontal = 1;
			// D-pad fallback (vertical)
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadUp))
				vertical = -1;
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadDown))
				vertical = 1;
		}

		// Emit movement signal (as Vector2)
		var moveVec = new Vector2(horizontal, vertical);

		// Clamp to max magnitude 1 to avoid faster diagonal movement when multiple digital inputs are pressed
		if (moveVec.Length() > 1f)
		{
			moveVec = moveVec.Normalized();
		}
		if (moveVec != _previousMove)
		{
			EmitSignal(SignalName.MoveInput, moveVec);
			if (EnableDebugLogging)
			{
				string source = Input.GetConnectedJoypads().Count > GamepadDevice ? "Gamepad" : "Keyboard";
				GD.Print($"[InputController] Movement: {moveVec.X:F2}, {moveVec.Y:F2} (Source: {source})");
			}
			_previousMove = moveVec;
		}

		// Jump input - keyboard (Space) or gamepad (A button)
		bool isJumpPressed = Input.IsKeyPressed(Key.Space);

		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isJumpPressed = isJumpPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A);
		}

		// Emit jump signals
		if (isJumpPressed && !_wasJumpPressed)
		{
			EmitSignal(SignalName.JumpPressed);
			if (EnableDebugLogging)
			{
				string source = Input.GetConnectedJoypads().Count > GamepadDevice && Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A) ? "Gamepad" : "Keyboard";
				GD.Print($"[InputController] Jump Pressed (Source: {source})");
			}
		}
		else if (!isJumpPressed && _wasJumpPressed)
		{
			EmitSignal(SignalName.JumpReleased);
			if (EnableDebugLogging)
			{
				GD.Print("[InputController] Jump Released");
			}
		}

		_wasJumpPressed = isJumpPressed;

		// Action input - keyboard (E or X) or gamepad (B button)
		bool isActionPressed = Input.IsKeyPressed(Key.E) || Input.IsKeyPressed(Key.X);

		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isActionPressed = isActionPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B);
		}

		// Emit action signals
		if (isActionPressed && !_wasActionPressed)
		{
			EmitSignal(SignalName.ActionPressed);
			if (EnableDebugLogging)
			{
				string source = Input.GetConnectedJoypads().Count > GamepadDevice && Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B) ? "Gamepad" : "Keyboard";
				GD.Print($"[InputController] Action Pressed (Source: {source})");
			}
		}
		else if (!isActionPressed && _wasActionPressed)
		{
			EmitSignal(SignalName.ActionReleased);
			if (EnableDebugLogging)
			{
				GD.Print("[InputController] Action Released");
			}
		}

		_wasActionPressed = isActionPressed;
	}

	// Helper methods for direct polling (alternative to signals)
	public Vector2 GetMoveVector()
	{
		return _previousMove;
	}

	public bool IsJumpPressed()
	{
		return _wasJumpPressed;
	}

	public bool IsJumpJustPressed()
	{
		bool isPressed = Input.IsKeyPressed(Key.Space);
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isPressed = isPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A);
		}
		return isPressed && !_wasJumpPressed;
	}

	public bool IsJumpJustReleased()
	{
		bool isPressed = Input.IsKeyPressed(Key.Space);
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isPressed = isPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A);
		}
		return !isPressed && _wasJumpPressed;
	}

	public bool IsActionPressed()
	{
		return _wasActionPressed;
	}

	public bool IsActionJustPressed()
	{
		bool isPressed = Input.IsKeyPressed(Key.E) || Input.IsKeyPressed(Key.X);
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isPressed = isPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B);
		}
		return isPressed && !_wasActionPressed;
	}

	public bool IsActionJustReleased()
	{
		bool isPressed = Input.IsKeyPressed(Key.E) || Input.IsKeyPressed(Key.X);
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			isPressed = isPressed || Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B);
		}
		return !isPressed && _wasActionPressed;
	}
}
