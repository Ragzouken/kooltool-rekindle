using UnityEngine;

public class ImageGridView : ViewComponent<ImageGrid> 
{
    [SerializeField] private SpriteRenderer cellPrefab;
    [SerializeField] private Transform cellParent;

    private MonoBehaviourPooler<IntVector2, SpriteRenderer> cells;

    private void Awake()
    {
        cells = new MonoBehaviourPooler<IntVector2, SpriteRenderer>(cellPrefab,
                                                               cellParent,
                                                               InitCell);
    }

    private void InitCell(IntVector2 cell, SpriteRenderer renderer)
    {
        renderer.transform.localPosition = cell * model.cellSize;
        renderer.sprite = model.cells[cell];
    }

    public void Setup(ImageGrid grid)
    {
        model = grid;

        Refresh();
    }

    public void Refresh()
    {
        cells.SetActive(model.cells.Keys);
        cells.MapActive(InitCell);
    }
}
