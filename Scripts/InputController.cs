using Godot;
using System;

public partial class InputController : Node
{
	#region Signals
	[Signal] public delegate void MoveInputEventHandler(Vector2 direction);
	[Signal] public delegate void JumpPressedEventHandler();
	[Signal] public delegate void JumpReleasedEventHandler();
	[Signal] public delegate void ActionPressedEventHandler();
	[Signal] public delegate void ActionReleasedEventHandler();
	[Signal] public delegate void AnyKeyPressedEventHandler();
	#endregion

	#region Export Properties
	[Export] public int GamepadDevice = 0;
	[Export] public float DeadZone = 0.2f;
	[Export] public bool EnableDebugLogging = false;
	#endregion

	#region Private State
	private Vector2 _previousMove = Vector2.Zero;
	private bool _wasJumpPressed;
	private bool _wasActionPressed;
	private bool _hadAnyInput = false;
	#endregion

	#region Public Properties
	public Vector2 Move => _previousMove;
	public bool IsJumpPressed => _wasJumpPressed;
	public bool IsActionPressed => _wasActionPressed;
	#endregion

	#region Main Processing
	public override void _Process(double delta)
	{
		bool hasAnyInput = false;
		
		// Gather input from all sources
		var moveVec = GetMovementInput();
		bool isJumpPressed = GetJumpInput();
		bool isActionPressed = GetActionInput();

		// Process and emit signals
		hasAnyInput |= ProcessMovementInput(moveVec);
		hasAnyInput |= ProcessJumpInput(isJumpPressed);
		hasAnyInput |= ProcessActionInput(isActionPressed);

		// Emit any key pressed signal
		if (hasAnyInput)
		{
			EmitAnyKeyPressed();
		}
	}
	#endregion

	#region Input Gathering
	private Vector2 GetMovementInput()
	{
		Vector2 movement = GetKeyboardMovement();
		
		// Override with gamepad if connected
		if (HasGamepad())
		{
			Vector2 gamepadMovement = GetGamepadMovement();
			if (gamepadMovement.Length() > 0.01f)
			{
				movement = gamepadMovement;
			}
		}

		// Normalize to prevent faster diagonal movement
		return movement.Length() > 1f ? movement.Normalized() : movement;
	}

	private Vector2 GetKeyboardMovement()
	{
		float horizontal = 0;
		float vertical = 0;

		if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A)) horizontal -= 1;
		if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D)) horizontal += 1;
		if (Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.W)) vertical -= 1;
		if (Input.IsKeyPressed(Key.Down) || Input.IsKeyPressed(Key.S)) vertical += 1;

		return new Vector2(horizontal, vertical);
	}

	private Vector2 GetGamepadMovement()
	{
		float horizontal = 0;
		float vertical = 0;

		// Analog stick input
		float stickX = Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftX);
		float stickY = Input.GetJoyAxis(GamepadDevice, JoyAxis.LeftY);
		
		if (Mathf.Abs(stickX) > DeadZone) horizontal = stickX;
		if (Mathf.Abs(stickY) > DeadZone) vertical = stickY;

		// D-pad fallback
		if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadLeft)) horizontal = -1;
		if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadRight)) horizontal = 1;
		if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadUp)) vertical = -1;
		if (Input.IsJoyButtonPressed(GamepadDevice, JoyButton.DpadDown)) vertical = 1;

		return new Vector2(horizontal, vertical);
	}

	private bool GetJumpInput()
	{
		bool isPressed = Input.IsKeyPressed(Key.Space);
		if (HasGamepad())
		{
			isPressed |= Input.IsJoyButtonPressed(GamepadDevice, JoyButton.A);
		}
		return isPressed;
	}

	private bool GetActionInput()
	{
		bool isPressed = Input.IsKeyPressed(Key.E) || Input.IsKeyPressed(Key.X);
		if (HasGamepad())
		{
			isPressed |= Input.IsJoyButtonPressed(GamepadDevice, JoyButton.B);
		}
		return isPressed;
	}

	private bool HasGamepad()
	{
		return Input.GetConnectedJoypads().Count > GamepadDevice;
	}
	#endregion

	#region Input Processing
	private bool ProcessMovementInput(Vector2 moveVec)
	{
		if (moveVec == _previousMove) return false;

		EmitSignal(SignalName.MoveInput, moveVec);
		LogInput($"Movement: {moveVec.X:F2}, {moveVec.Y:F2}");
		_previousMove = moveVec;

		return moveVec.Length() > 0.01f;
	}

	private bool ProcessJumpInput(bool isJumpPressed)
	{
		bool hasInput = false;

		if (isJumpPressed && !_wasJumpPressed)
		{
			EmitSignal(SignalName.JumpPressed);
			LogInput("Jump Pressed");
			hasInput = true;
		}
		else if (!isJumpPressed && _wasJumpPressed)
		{
			EmitSignal(SignalName.JumpReleased);
			LogInput("Jump Released");
		}

		_wasJumpPressed = isJumpPressed;
		return hasInput;
	}

	private bool ProcessActionInput(bool isActionPressed)
	{
		bool hasInput = false;

		if (isActionPressed && !_wasActionPressed)
		{
			EmitSignal(SignalName.ActionPressed);
			LogInput("Action Pressed");
			hasInput = true;
		}
		else if (!isActionPressed && _wasActionPressed)
		{
			EmitSignal(SignalName.ActionReleased);
			LogInput("Action Released");
		}

		_wasActionPressed = isActionPressed;
		return hasInput;
	}

	private void EmitAnyKeyPressed()
	{
		EmitSignal(SignalName.AnyKeyPressed);
		_hadAnyInput = true;
		LogInput("Any key pressed - first input detected");
	}
	#endregion

	#region Public API Methods
	// Direct polling methods (alternative to signals)
	public Vector2 GetMoveVector() => _previousMove;

	public bool IsJumpJustPressed
	{
		get
		{
			bool currentlyPressed = GetJumpInput();
			return currentlyPressed && !_wasJumpPressed;
		}
	}

	public bool IsJumpJustReleased
	{
		get
		{
			bool currentlyPressed = GetJumpInput();
			return !currentlyPressed && _wasJumpPressed;
		}
	}

	public bool IsActionJustPressed
	{
		get
		{
			bool currentlyPressed = GetActionInput();
			return currentlyPressed && !_wasActionPressed;
		}
	}

	public bool IsActionJustReleased
	{
		get
		{
			bool currentlyPressed = GetActionInput();
			return !currentlyPressed && _wasActionPressed;
		}
	}
	#endregion

	#region Utility Methods
	private void LogInput(string message)
	{
		if (!EnableDebugLogging) return;
		
		string source = HasGamepad() ? "Gamepad" : "Keyboard";
		GD.Print($"[InputController] {message} (Source: {source})");
	}
	#endregion
}
