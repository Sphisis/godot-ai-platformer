using Godot;
using System;

[Tool]
public partial class TileDepthCalculator : TileMapLayer
{
	[Export] bool Active;

	[Export]
	public int BlurRadius = 10;
	
	[Export(PropertyHint.Range, "1,3,1")]
	public int BlurQuality = 2; // 1=fast, 2=balanced, 3=quality
	
	[Export(PropertyHint.Range, "0.0,1.0")]
	public float AmbientLight = 0.1f;
	
	[Export]
	public float LightIntensity = 1.0f;
	
	[Export]
	public Color LightColor = new Color(1, 1, 1);
	
	[Export]
	public Color DarkColor = new Color(0, 0, 0);
	
	[Export(PropertyHint.Range, "0.25,1.0,0.25")]
	public float LightMapResolutionScale = 0.5f;
	
	private ShaderMaterial _material;
	private ImageTexture _lightMapTexture;
	private Camera2D _camera;
	private Rect2I _lastCameraRect = new Rect2I();
	private byte[] _imageDataBuffer;
	private byte[] _lightBytes = BitConverter.GetBytes(1.0f);
	private byte[] _darkBytes = BitConverter.GetBytes(0.0f);

	public override void _EnterTree()
	{
		if (Engine.IsEditorHint())
		{
			GD.Print("[TileDepthCalculator] _EnterTree called in editor");
			SetProcess(true);
		}
	}

	public override void _Ready()
	{
		GD.Print("[TileDepthCalculator] _Ready called, IsEditor: " + Engine.IsEditorHint());
		SetProcess(true);
		SetupMaterial();
	}
	
	private void SetupMaterial()
	{
		if (TileSet == null)
		{
			if (Engine.IsEditorHint())
			{
				GD.Print("[TileDepthCalculator] TileSet not assigned yet");
			}
			return;
		}
		
		if (Material is ShaderMaterial shaderMat)
		{
			_material = shaderMat;
		}
		else
		{
			_material = new ShaderMaterial();
			var shader = GD.Load<Shader>("res://Shaders/depth_fade.gdshader");
			if (shader == null)
			{
				GD.PushError("[TileDepthCalculator] Failed to load depth_fade shader");
				return;
			}
			_material.Shader = shader;
			Material = _material;
		}
	}
	
	public override void _Process(double delta)
	{
		_material.SetShaderParameter("ambient_light", AmbientLight);
		_material.SetShaderParameter("light_intensity", LightIntensity);
		_material.SetShaderParameter("light_color", LightColor);
		_material.SetShaderParameter("dark_color", DarkColor);

		// Handle Active flag - remove material when inactive
		if (!Active)
		{
			if (Material != null)
			{
				Material = null;
			}
			return;
		}
		
		// Setup material if needed
		if (_material == null)
		{
			if (Engine.IsEditorHint())
			{
				GD.Print("[TileDepthCalculator] Setting up material in editor");
			}
			SetupMaterial();
			if (_material == null) return;
		}
		
		// Ensure material is applied
		if (Material != _material)
		{
			Material = _material;
		}

		_camera = GetViewport()?.GetCamera2D();
		
		// In editor, calculate for entire tilemap if no camera
		Rect2I cameraRect;
		if (_camera == null && Engine.IsEditorHint())
		{
			cameraRect = GetUsedRect();
			if (Engine.IsEditorHint() && _lastCameraRect.Size == new Vector2I(0, 0))
			{
				GD.Print($"[TileDepthCalculator] Editor mode - calculating for entire tilemap: {cameraRect}");
			}
		}
		else if (_camera == null)
		{
			return;
		}
		else
		{
			cameraRect = GetCameraVisibleRect();
		}
		
		// Only recalculate if camera moved significantly (at least 1 tile) or first frame
		bool rectChanged = cameraRect.Position != _lastCameraRect.Position || 
		                   cameraRect.Size != _lastCameraRect.Size;
		
		if (rectChanged)
		{
			_lastCameraRect = cameraRect;
			CalculateVisibleLightMap(cameraRect);
		}
	}
	
	private Rect2I GetCameraVisibleRect()
	{
		if (_camera == null) return new Rect2I();
		
		var viewport = GetViewport();
		if (viewport == null) return new Rect2I();
		
		var viewportSize = viewport.GetVisibleRect().Size;
		var cameraPos = _camera.GlobalPosition;
		var zoom = _camera.Zoom;
		
		// Calculate world-space visible area
		var halfSize = viewportSize / (zoom * 2.0f);
		var topLeft = cameraPos - halfSize;
		var bottomRight = cameraPos + halfSize;
		
		// Convert to tile coordinates
		int tileSize = TileSet?.TileSize.X ?? 16;
		var tileTopLeft = new Vector2I(
			Mathf.FloorToInt(topLeft.X / tileSize),
			Mathf.FloorToInt(topLeft.Y / tileSize)
		);
		var tileBottomRight = new Vector2I(
			Mathf.CeilToInt(bottomRight.X / tileSize),
			Mathf.CeilToInt(bottomRight.Y / tileSize)
		);
		
		// Add padding for light bleeding
		int padding = Mathf.CeilToInt(BlurRadius / (float)tileSize) + 2;
		tileTopLeft -= new Vector2I(padding, padding);
		tileBottomRight += new Vector2I(padding, padding);
		
		// Clamp to used rect
		var usedRect = GetUsedRect();
		tileTopLeft = new Vector2I(
			Mathf.Max(tileTopLeft.X, usedRect.Position.X),
			Mathf.Max(tileTopLeft.Y, usedRect.Position.Y)
		);
		tileBottomRight = new Vector2I(
			Mathf.Min(tileBottomRight.X, usedRect.End.X),
			Mathf.Min(tileBottomRight.Y, usedRect.End.Y)
		);
		
		var size = tileBottomRight - tileTopLeft;
		return new Rect2I(tileTopLeft, size);
	}
	
	private void CalculateVisibleLightMap(Rect2I bounds)
	{
		if (bounds.Size.X <= 0 || bounds.Size.Y <= 0)
		{
			return;
		}
		
		if (TileSet == null)
		{
			return;
		}
		
		int tileSize = TileSet.TileSize.X;
		float scale = Mathf.Clamp(LightMapResolutionScale, 0.25f, 1.0f);
		int pixelSize = Mathf.Max(1, Mathf.RoundToInt(tileSize * scale));
		int width = bounds.Size.X * pixelSize;
		int height = bounds.Size.Y * pixelSize;
		
		// Prepare bulk data buffer (Format.Rf uses 4 bytes per pixel for float)
		int bufferSize = width * height * 4;
		if (_imageDataBuffer == null || _imageDataBuffer.Length != bufferSize)
		{
			_imageDataBuffer = new byte[bufferSize];
		}
		
		// Fill data buffer: empty tiles = white (light), solid tiles = black (dark)
		int rowStride = width * 4;
		int tileRowBytes = pixelSize * 4; // Bytes per tile row
		
		for (int tileY = 0; tileY < bounds.Size.Y; tileY++)
		{
			int baseY = tileY * pixelSize;
			for (int tileX = 0; tileX < bounds.Size.X; tileX++)
			{
				var pos = new Vector2I(bounds.Position.X + tileX, bounds.Position.Y + tileY);
				var cellData = GetCellTileData(pos);
				byte[] sourceBytes = cellData == null ? _lightBytes : _darkBytes;
				
				int baseTileIndex = (baseY * width + tileX * pixelSize) * 4;
				
				// Fill first row of tile pixels using Span for better performance
				Span<byte> buffer = _imageDataBuffer;
				byte b0 = sourceBytes[0], b1 = sourceBytes[1], b2 = sourceBytes[2], b3 = sourceBytes[3];
				
				for (int px = 0; px < pixelSize; px++)
				{
					int index = baseTileIndex + px * 4;
					buffer[index] = b0;
					buffer[index + 1] = b1;
					buffer[index + 2] = b2;
					buffer[index + 3] = b3;
				}
				
				// Copy first row to remaining rows using Buffer.BlockCopy
				for (int py = 1; py < pixelSize; py++)
				{
					int srcIndex = baseTileIndex;
					int dstIndex = baseTileIndex + py * rowStride;
					Buffer.BlockCopy(_imageDataBuffer, srcIndex, _imageDataBuffer, dstIndex, tileRowBytes);
				}
			}
		}
		
		// Create image from bulk data
		var image = Image.CreateFromData(width, height, false, Image.Format.Rf, _imageDataBuffer);
		
		// No CPU blur - let the shader handle it
		// Check if texture needs to be recreated due to size change
		if (_lightMapTexture == null || _lightMapTexture.GetWidth() != width || _lightMapTexture.GetHeight() != height)
		{
			_lightMapTexture = ImageTexture.CreateFromImage(image);
		}
		else
		{
			_lightMapTexture.Update(image);
		}
		
		_material.SetShaderParameter("light_texture", _lightMapTexture);
		_material.SetShaderParameter("tilemap_offset", new Vector2(bounds.Position.X, bounds.Position.Y));
		_material.SetShaderParameter("tilemap_size", new Vector2(bounds.Size.X, bounds.Size.Y));
		_material.SetShaderParameter("tile_size", tileSize);
		_material.SetShaderParameter("blur_radius", (float)BlurRadius);
		_material.SetShaderParameter("blur_quality", BlurQuality);
	}

	private void UpdateMaterialParameters()
	{
		_material.SetShaderParameter("ambient_light", AmbientLight);
		_material.SetShaderParameter("light_intensity", LightIntensity);
		_material.SetShaderParameter("light_color", LightColor);
		_material.SetShaderParameter("dark_color", DarkColor);
	}
}
