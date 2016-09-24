using UnityEngine;

public class ImageGridView : InstanceView<ImageGrid> 
{
    [SerializeField] private ImageCellView cellPrefab;
    [SerializeField] private Transform cellParent;

    private InstancePool<IntVector2, ImageCellView> cells;

    private void Awake()
    {
        cells = new InstancePool<IntVector2, ImageCellView>(cellPrefab, cellParent);
    }

    public override void Configure(ImageGrid grid)
    {
        base.Configure(grid);

        Refresh();
    }

    public override void Refresh()
    {
        cells.SetActive(config.cells.Keys);
        cells.Refresh();
    }
}
