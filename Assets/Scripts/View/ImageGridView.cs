using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ImageGridView : ViewComponent<ImageGrid> 
{
    [SerializeField] private SpriteRenderer cellPrefab;
    [SerializeField] private Transform cellParent;

    private MonoBehaviourPooler<Point, SpriteRenderer> cells;

    private void Awake()
    {
        cells = new MonoBehaviourPooler<Point, SpriteRenderer>(cellPrefab,
                                                               cellParent,
                                                               InitCell);
    }

    private void InitCell(Point cell, SpriteRenderer renderer)
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
