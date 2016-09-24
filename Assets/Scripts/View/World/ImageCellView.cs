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

    protected override void Configure()
    {
        renderer.transform.localPosition = config * grid.config.cellSize;
        renderer.sprite = grid.config.cells[config];
    }

    public override void Refresh()
    {
        renderer.sprite = grid.config.cells[config];
    }
}
