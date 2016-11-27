using UnityEngine;

using kooltool;

public class TileMapView : InstanceView<TileMap> 
{
    [SerializeField] private InstancePoolSetup cellSetup;

    private InstancePool<IntVector2> cells;

    private void Awake()
    {
        cells = cellSetup.Finalise<IntVector2>();
    }

    public override void Refresh()
    {
        cells.SetActive(config.tiles.Keys);
        cells.Refresh();
    }
}
