using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BasicDrawing : MonoBehaviour 
{
    [SerializeField] private SpriteRenderer drawing;

    private Texture2D texture;
    private new Collider collider;

    private bool dragging;
    private Vector2 prevMouse, nextMouse;

    private void Awake()
    {
        texture = Texture2DExtensions.Blank(512, 512, Color.clear);
        drawing.sprite = texture.FullSprite(pixelsPerUnit: 512);

        collider = drawing.GetComponent<Collider>();
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == collider)
            {
                nextMouse = (Vector2) hit.point * 512;
                nextMouse = nextMouse.Floored();
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!dragging)
            {
                dragging = true;
            }
            else
            {
                var circle = Brush.Circle(Random.Range(1, 6), 
                                          Color.HSVToRGB(Random.value, 0.75f, 1f));

                var sweep = Brush.Sweep(circle, prevMouse, nextMouse);
                drawing.sprite.Brush(sweep.AsBrush(Vector2.zero, Blend.alpha));
                drawing.sprite.Apply();
            }
        }
        else
        {
            dragging = false;
        }

        prevMouse = nextMouse;
    }
}
