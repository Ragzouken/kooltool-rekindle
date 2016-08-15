using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BasicDrawing : MonoBehaviour 
{
    [SerializeField] private SpriteRenderer drawing;

    private ManagedSprite<Color> sprite;

    private new Collider collider;

    private bool dragging;
    private Vector2 prevMouse, nextMouse;

    private void Awake()
    {
        sprite = TextureColor.Pooler.Instance.GetSprite(512, 512);
        sprite.SetPixelsPerUnit(512);
        sprite.Clear(Color.clear);
        sprite.mTexture.Apply();

        drawing.sprite = sprite.uSprite;

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
                int thickness = Random.Range(1, 6);
                Color color = Color.HSVToRGB(Random.value, 0.75f, 1f);

                var line = TextureColor.Pooler.Instance.Line(prevMouse, nextMouse, color, thickness, TextureColor.alpha);

                sprite.Blend(line, TextureColor.alpha);
                sprite.mTexture.Apply();

                TextureColor.Pooler.Instance.FreeTexture(line.mTexture);
                TextureColor.Pooler.Instance.FreeSprite(line);
            }
        }
        else
        {
            dragging = false;
        }

        prevMouse = nextMouse;
    }
}
