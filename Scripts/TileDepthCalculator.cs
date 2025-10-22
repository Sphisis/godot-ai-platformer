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
	
	private ShaderMaterial _material;
	private ImageTexture _lightMapTexture;

	public override void _Ready()
	{
		CallDeferred(nameof(CalculateAndApply));
	}
	
	public void CalculateAndApply()
	{
		Rect2I usedRect = GetUsedRect();
		if (usedRect.Size.X == 0 || usedRect.Size.Y == 0)
		{
			GD.PushWarning("[TileDepthCalculator] TileMap is empty");
			return;
		}
		
		CreateLightMap(usedRect);
		ApplyToMaterial(usedRect);
	}
	
	private void CreateLightMap(Rect2I bounds)
	{
		int tileSize = TileSet.TileSize.X;
		int width = bounds.Size.X * tileSize;
		int height = bounds.Size.Y * tileSize;
		
		var image = Image.CreateEmpty(width, height, false, Image.Format.Rf);
		
		// Fill image: empty tiles = white (light), solid tiles = black (dark)
		for (int tileX = 0; tileX < bounds.Size.X; tileX++)
		{
			for (int tileY = 0; tileY < bounds.Size.Y; tileY++)
			{
				var pos = new Vector2I(bounds.Position.X + tileX, bounds.Position.Y + tileY);
				var cellData = GetCellTileData(pos);
				float lightValue = cellData == null ? 1.0f : 0.0f;
				
				for (int px = 0; px < tileSize; px++)
				{
					for (int py = 0; py < tileSize; py++)
					{
						int pixelX = tileX * tileSize + px;
						int pixelY = tileY * tileSize + py;
						image.SetPixel(pixelX, pixelY, new Color(lightValue, 0, 0, 1));
					}
				}
			}
		}
		
		// Apply blur
		var blurred = ApplyGaussianBlur(image, BlurRadius);
		_lightMapTexture = ImageTexture.CreateFromImage(blurred);
	}
	
	private Image ApplyGaussianBlur(Image source, int radius)
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
	}
	
	private void ApplyToMaterial(Rect2I usedRect)
	{
		if (_lightMapTexture == null) return;
		
		var material = Material;
		if (material is ShaderMaterial shaderMat)
		{
			_material = shaderMat;
		}
		else
		{
			_material = new ShaderMaterial();
			var shader = GD.Load<Shader>("res://Shaders/depth_fade.gdshader");
			if (shader == null) return;
			_material.Shader = shader;
			Material = _material;
		}
		
		_material.SetShaderParameter("light_texture", _lightMapTexture);
		_material.SetShaderParameter("tilemap_offset", new Vector2(usedRect.Position.X, usedRect.Position.Y));
		_material.SetShaderParameter("tilemap_size", new Vector2(usedRect.Size.X, usedRect.Size.Y));
		_material.SetShaderParameter("tile_size", TileSet.TileSize.X);
		_material.SetShaderParameter("ambient_light", AmbientLight);
		_material.SetShaderParameter("light_intensity", LightIntensity);
		_material.SetShaderParameter("light_color", LightColor);
	}
}
