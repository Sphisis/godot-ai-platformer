using Godot;

public partial class ParallaxScroller : Sprite2D
{
	[Export] public float ScrollScale = 0.5f;
	[Export] public Vector2 ViewportSize = new Vector2(1920, 1080);

	private Camera2D _camera;
	private Vector2 _previousCameraPosition;

	public override void _Ready()
	{
		_camera = GetParent<Camera2D>();
		if (_camera != null)
		{
			_previousCameraPosition = _camera.GlobalPosition;
		}

		// Enable region and set initial size
		RegionEnabled = true;
		if (RegionRect.Size == Vector2.Zero)
		{
			RegionRect = new Rect2(Vector2.Zero, ViewportSize);
		}

		// Enable texture repeat
		if (Texture is Texture2D tex2D)
		{
			var image = tex2D.GetImage();
			if (image != null)
			{
				var newTexture = ImageTexture.CreateFromImage(image);
				Texture = newTexture;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_camera == null) return;

		Vector2 currentCameraPosition = _camera.GlobalPosition;
		Vector2 cameraMovement = currentCameraPosition - _previousCameraPosition;

		// Scroll texture by offsetting region rect
		RegionRect = new Rect2(
			RegionRect.Position + cameraMovement * ScrollScale,
			RegionRect.Size
		);

		_previousCameraPosition = currentCameraPosition;
	}
}
