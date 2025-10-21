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
        _isGameOver = false;
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
        if (_label == null || _isGameOver) return;  // Stop blinking when game is over

        // Update time
        _time += (float)delta;
        
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
