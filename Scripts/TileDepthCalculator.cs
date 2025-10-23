using Godot;
using System;

public partial class TileDepthCalculator : TileMapLayer
{
	[Export]
	public int BlurRadius = 10;
	
	[Export(PropertyHint.Range, "0.0,1.0")]
	public float AmbientLight = 0.1f;
	
	[Export]
	public float LightIntensity = 1.0f;
	
	[Export]
	public Color LightColor = new Color(1, 1, 1);
	
	[Export(PropertyHint.Range, "0.25,1.0,0.25")]
	public float LightMapResolutionScale = 0.5f;
	
	private ShaderMaterial _material;
	private ImageTexture _lightMapTexture;
	private Camera2D _camera;
	private Rect2I _lastCameraRect = new Rect2I();

	public override void _Ready()
	{
		SetProcess(true);
		SetupMaterial();
	}
	
	private void SetupMaterial()
	{
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
		
		_material.SetShaderParameter("ambient_light", AmbientLight);
		_material.SetShaderParameter("light_intensity", LightIntensity);
		_material.SetShaderParameter("light_color", LightColor);
	}
	
	public override void _Process(double delta)
	{
		_camera = GetViewport()?.GetCamera2D();
		if (_camera == null)
		{
			return;
		}
		
		var cameraRect = GetCameraVisibleRect();
		
		// Only recalculate if camera moved significantly or first frame
		if (cameraRect != _lastCameraRect)
		{
			_lastCameraRect = cameraRect;
			CalculateVisibleLightMap(cameraRect);
		}
		
		UpdateMaterialParameters();
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
		
		int tileSize = TileSet.TileSize.X;
		float scale = Mathf.Clamp(LightMapResolutionScale, 0.25f, 1.0f);
		int pixelSize = Mathf.Max(1, Mathf.RoundToInt(tileSize * scale));
		int width = bounds.Size.X * pixelSize;
		int height = bounds.Size.Y * pixelSize;
		
		var image = Image.CreateEmpty(width, height, false, Image.Format.Rf);
		
		// Fill image: empty tiles = white (light), solid tiles = black (dark)
		for (int tileX = 0; tileX < bounds.Size.X; tileX++)
		{
			for (int tileY = 0; tileY < bounds.Size.Y; tileY++)
			{
				var pos = new Vector2I(bounds.Position.X + tileX, bounds.Position.Y + tileY);
				var cellData = GetCellTileData(pos);
				float lightValue = cellData == null ? 1.0f : 0.0f;
				
				for (int px = 0; px < pixelSize; px++)
				{
					for (int py = 0; py < pixelSize; py++)
					{
						int pixelX = tileX * pixelSize + px;
						int pixelY = tileY * pixelSize + py;
						image.SetPixel(pixelX, pixelY, new Color(lightValue, 0, 0, 1));
					}
				}
			}
		}
		
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
	}
	
	/*private Image ApplyGaussianBlur(Image source, int radius)
	{
		if (radius <= 0) return source;
		
		int width = source.GetWidth();
		int height = source.GetHeight();
		var temp = Image.CreateEmpty(width, height, false, Image.Format.Rf);
		var result = Image.CreateEmpty(width, height, false, Image.Format.Rf);
		
		// Horizontal pass
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float sum = 0f;
				float weightSum = 0f;
				
				for (int i = -radius; i <= radius; i++)
				{
					int sampleX = Mathf.Clamp(x + i, 0, width - 1);
					float weight = Mathf.Exp(-(i * i) / (2.0f * radius * radius));
					sum += source.GetPixel(sampleX, y).R * weight;
					weightSum += weight;
				}
				
				temp.SetPixel(x, y, new Color(sum / weightSum, 0, 0, 1));
			}
		}
		
		// Vertical pass
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float sum = 0f;
				float weightSum = 0f;
				
				for (int j = -radius; j <= radius; j++)
				{
					int sampleY = Mathf.Clamp(y + j, 0, height - 1);
					float weight = Mathf.Exp(-(j * j) / (2.0f * radius * radius));
					sum += temp.GetPixel(x, sampleY).R * weight;
					weightSum += weight;
				}
				
				result.SetPixel(x, y, new Color(sum / weightSum, 0, 0, 1));
			}
		}
		
		return result;
	}*/
	
	private void UpdateMaterialParameters()
	{
		_material.SetShaderParameter("ambient_light", AmbientLight);
		_material.SetShaderParameter("light_intensity", LightIntensity);
		_material.SetShaderParameter("light_color", LightColor);
	}
}
