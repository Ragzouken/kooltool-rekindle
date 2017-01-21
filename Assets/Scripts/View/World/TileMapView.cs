using UnityEngine;
using System.Collections.Generic;

using kooltool;

public class TileMapView : InstanceView<TileMap> 
{
    [SerializeField] private InstancePoolSetup cellSetup;

    private InstancePool<IntVector2> cells;

    public CameraController camera;
    public IntRect clipping;
    private List<IntVector2> clipped = new List<IntVector2>(81);

    private void Awake()
    {
        cells = cellSetup.Finalise<IntVector2>();
    }

    public override void Refresh()
    {
        var center = ((IntVector2) camera.focus).CellCoords(32);
        clipping.xMin = center.x - 4;
        clipping.yMin = center.y - 4;
        clipping.xMax = center.x + 4;
        clipping.yMax = center.y + 4;

        clipped.Clear();

        IntVector2 cell;

        for (int y = clipping.yMin; y <= clipping.yMax; ++y)
        {
            for (int x = clipping.xMin; x <= clipping.xMax; ++x)
            {
                cell.x = x;
                cell.y = y;

                if (config.tiles.ContainsKey(cell))
                {
                    clipped.Add(cell);
                }
            }
        }

        cells.SetActive(clipped);
        cells.Refresh();
    }
}
