using Godot;
using System;
using System.Collections.Generic;

public partial class MazeController : TileMapLayer
{
    [Export]
    public PackedScene CollectibleScene;  // Reference to the collectible prefab

    [Export]
    public Vector2 CollectibleOffset = new Vector2(0, 0);  // Offset from tile center


	private List<Vector2I> _floorCells = new List<Vector2I>();
	
	public override void _Ready()
	{
		if (CollectibleScene == null)
		{
			GD.PushWarning($"{nameof(MazeController)}: {nameof(CollectibleScene)} is not set.");
			return;
		}

		_floorCells.Add(new Vector2I(4, 3)); // Floor tile atlas coordinates

		SpawnCollectiblesOnFloor();
	}

    private void SpawnCollectiblesOnFloor()
    {
        // Get the used cells in the tilemap
        var cells = GetUsedCells();
        if (cells.Count == 0)
        {
            GD.PushWarning("[MazeController] No tiles found in tilemap!");
            return;
        }

        // Check each cell
        foreach (Vector2I cell in cells)
        {
            // Get the tile ID at this cell
            Vector2I atlasCoords = GetCellAtlasCoords(cell);
            
            // Check if it's a floor tile
            if (_floorCells.Contains(atlasCoords))
			{
				SpawnCollectibleAt(cell);
            }
        }
    }

    private void SpawnCollectibleAt(Vector2I cell)
    {
        // Instance the collectible
        Node2D collectible = CollectibleScene.Instantiate() as Node2D;
        if (collectible == null)
        {
            GD.PushWarning("[MazeController] Failed to instantiate collectible!");
            return;
        }

        // Position at cell center + offset
        Vector2 worldPos = MapToLocal(cell) + CollectibleOffset;
		collectible.GlobalPosition = ToGlobal(worldPos);
		
        // Add to scene tree
        AddChild(collectible);
    }
}
