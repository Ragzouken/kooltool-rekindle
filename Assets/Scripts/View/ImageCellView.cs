using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ImageCellView : InstanceView<IntVector2>
{
    [SerializeField] new private SpriteRenderer renderer;
    [SerializeField] private ImageGridView grid;

    public override void Configure(IntVector2 cell)
    {
        base.Configure(cell);

        renderer.transform.localPosition = cell * grid.model.cellSize;
        renderer.sprite = grid.model.cells[cell];
        renderer.sortingLayerName = "World - Background";
    }

    public override void Refresh()
    {
        base.Refresh();

        renderer.sprite = grid.model.cells[config];
    }
}
