using Godot;
using System;

public partial class TextController : Label
{
    [Export]
    public NodePath LabelPath;

    [ExportGroup("Alarm Effect")]
    [Export(PropertyHint.Range, "0.1,2.0,0.1")]
    public float BlinkSpeed = 0.5f;  // Time for one complete on/off cycle

    [Export(PropertyHint.Range, "0.0,1.0,0.1")]
    public float OnRatio = 0.5f;     // Portion of the cycle the text is visible (0-1)

    [Export]
    public Color AlarmColor = Colors.Red;  // Color when visible

    [Export(PropertyHint.Range, "0.0,1.0,0.1")]
    public float MinAlpha = 0.0f;    // Minimum visibility (0 = fully off)

    [Export(PropertyHint.Range, "0.0,1.0,0.1")]
    public float MaxAlpha = 1.0f;    // Maximum visibility

    private Label _label;
    private float _time;
    private bool _isGameOver = false;
    private bool _isVictory = false;
    private GameManager _gameManager;

    public override void _Ready()
    {
        _label = GetNode<Label>(LabelPath);
        if (_label == null)
        {
            GD.PushWarning("[TextController] No Label found at path: " + LabelPath);
            QueueFree();
            return;
        }

        // Store the original color but use our alarm color
        _label.Modulate = new Color(AlarmColor, MinAlpha);

        // Connect to GameManager
        _gameManager = GetNode<GameManager>("/root/GameManager");
        if (_gameManager != null)
        {
            _gameManager.PlayerDeath += OnGameOver;
            _gameManager.ResetLevel += OnReset;
            _gameManager.PlayerVictory += OnVictory;
        }
        else
        {
            GD.PushWarning("[TextController] GameManager not found");
        }
    }

    private void OnReset()
    {
        _label.Text = "RUN AGAIN!";
        FitTextToWidth();
        _label.Modulate = new Color(AlarmColor, MinAlpha);
        _label.Scale = Vector2.One;  // Reset scale
        _isGameOver = false;
        _isVictory = false;
        _time = 0f;
    }

    private void OnGameOver()
    {
        _isGameOver = true;
        if (_label != null)
        {
            _label.Text = "GAME OVER";
            FitTextToWidth();
            _label.Modulate = new Color(AlarmColor, MaxAlpha);  // Make fully visible
        }
    }

    private void OnVictory()
    {
        _isVictory = true;
        _isGameOver = true;  // Stop normal blinking
        _time = 0f;
        if (_label != null)
        {
            _label.Text = "YOU WIN!";
            FitTextToWidth();
        }
    }    
    
    private void FitTextToWidth()
    {
        if (string.IsNullOrEmpty(Text))
            return;

        var font = GetThemeFont("font");
        if (font == null)
            return;

        int fontSize = GetThemeFontSize("font_size");
        float availableWidth = Size.X;
        float textWidth = font.GetStringSize(Text, HorizontalAlignment.Left, -1, fontSize).X;

        // Decrease font size until it fits (basic approach)
        while (textWidth > availableWidth && fontSize > 5)
        {
            fontSize--;
            textWidth = font.GetStringSize(Text, HorizontalAlignment.Left, -1, fontSize).X;
        }

        AddThemeFontSizeOverride("font_size", fontSize);
    }

    public override void _Process(double delta)
    {
        if (_label == null) return;
        
        // Update time
        _time += (float)delta;
        
        // Victory color cycling (Commodore 64 style!)
        if (_isVictory)
        {
            // Fast color cycling through classic C64 palette
            float cycle = _time * 8.0f;  // Speed of color change
            int colorIndex = Mathf.FloorToInt(cycle) % 16;
            
            // Classic Commodore 64 color palette
            Color[] c64Colors = new Color[]
            {
                new Color(0.0f, 0.0f, 0.0f),       // Black
                new Color(1.0f, 1.0f, 1.0f),       // White
                new Color(0.53f, 0.0f, 0.0f),      // Red
                new Color(0.0f, 0.93f, 0.93f),     // Cyan
                new Color(0.6f, 0.0f, 0.6f),       // Purple
                new Color(0.0f, 0.8f, 0.0f),       // Green
                new Color(0.0f, 0.0f, 0.67f),      // Blue
                new Color(0.93f, 0.93f, 0.47f),    // Yellow
                new Color(0.6f, 0.4f, 0.0f),       // Orange
                new Color(0.4f, 0.27f, 0.0f),      // Brown
                new Color(0.8f, 0.47f, 0.47f),     // Light Red
                new Color(0.33f, 0.33f, 0.33f),    // Dark Grey
                new Color(0.47f, 0.47f, 0.47f),    // Grey
                new Color(0.67f, 1.0f, 0.67f),     // Light Green
                new Color(0.47f, 0.47f, 1.0f),     // Light Blue
                new Color(0.73f, 0.73f, 0.73f)     // Light Grey
            };
            
            _label.Modulate = c64Colors[colorIndex];
            
            // Pulsing scale effect
            float pulse = Mathf.Sin(_time * 4.0f) * 0.3f + 1.0f;  // Oscillate between 0.7 and 1.3
            _label.Scale = Vector2.One * 2.0f * pulse;  // Base 200% with pulse
            
            return;
        }
        
        if (_isGameOver) return;  // Stop normal blinking when game is over

        // Calculate the phase of the blink (0 to 1)
        float phase = Mathf.PosMod(_time / BlinkSpeed, 1.0f);
        
        // Sharp on/off transition
        float alpha;
        if (phase < OnRatio)
        {
            // On phase - full brightness
            alpha = MaxAlpha;
        }
        else
        {
            // Off phase - minimum brightness
            alpha = MinAlpha;
        }

        // Update the label's color, maintaining the RGB but changing alpha
        _label.Modulate = new Color(AlarmColor, alpha);
    }
}
