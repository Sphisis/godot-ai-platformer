using Godot;
using System;

public partial class InputController : Node
{
	// Input events
	[Signal] public delegate void MoveInputEventHandler(float direction);
	[Signal] public delegate void JumpPressedEventHandler();
	[Signal] public delegate void JumpReleasedEventHandler();
	[Signal] public delegate void ActionPressedEventHandler();
	[Signal] public delegate void ActionReleasedEventHandler();

	// Gamepad settings
	[Export] public int GamepadDevice = 0;
	[Export] public float DeadZone = 0.2f;
	[Export] public bool EnableDebugLogging = false;

	private float _previousHorizontal;
	private bool _wasJumpPressed;
	private bool _wasActionPressed;

	public override void _Process(double delta)
	{
		// Get horizontal input from keyboard or gamepad
		float horizontal = 0;

		// Keyboard input
		if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A))
			horizontal -= 1;
		if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D))
			horizontal += 1;

		// Gamepad input (left stick or D-pad)
		if (Input.GetConnectedJoypads().Count > GamepadDevice)
		{
			float stickX = Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftX);
			if (Mathf.Abs(stickX) > DeadZone)
			{
				horizontal = stickX;
			}

			// D-pad fallback
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadLeft))
				horizontal = -1;
			if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadRight))
				horizontal = 1;
		}

		// Emit movement signal
		if (horizontal != _previousHorizontal)
		{
			EmitSignal(SignalName.MoveInput, horizontal);
			if (EnableDebugLogging)
			{
				string source = Input.GetConnectedJoypads().Count > GamepadDevice ? "Gamepad" : "Keyboard";
				GD.Print($"[InputController] Movement: {horizontal:F2} (Source: {source})");
			}
			_previousHorizontal = horizontal;
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
	public float GetMoveDirection()
	{
		return _previousHorizontal;
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
