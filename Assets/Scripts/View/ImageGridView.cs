using UnityEngine;

public class ImageGridView : ViewComponent<ImageGrid> 
{
    [SerializeField] private ImageCellView cellPrefab;
    [SerializeField] private Transform cellParent;

    private InstancePool<IntVector2, ImageCellView> cells;

    private void Awake()
    {
        cells = new InstancePool<IntVector2, ImageCellView>(cellPrefab, cellParent);
    }

    public void Setup(ImageGrid grid)
    {
        model = grid;

        Refresh();
    }

    public void Refresh()
    {
        cells.SetActive(model.cells.Keys);
        cells.Refresh();
    }
}
