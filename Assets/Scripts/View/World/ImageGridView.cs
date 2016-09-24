using UnityEngine;

public class ImageGridView : InstanceView<ImageGrid> 
{
    [SerializeField] private InstancePoolSetup cellSetup;

    private InstancePool<IntVector2> cells;

    private void Awake()
    {
        cells = cellSetup.Finalise<IntVector2>();
    }

    public override void Refresh()
    {
        cells.SetActive(config.cells.Keys);
        cells.Refresh();
    }
}
