using UnityEngine;

public class BasicDrawing : MonoBehaviour 
{
    [SerializeField] private SpriteRenderer drawing;

    private ManagedSprite<Color32> sprite;

    private new Collider collider;

    private bool dragging;
    private Vector2 prevMouse, nextMouse;

    private void Awake()
    {
        sprite = TextureColor32.Pooler.Instance.GetSprite(512, 512);
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
                color.a = .75f;

                var line = TextureColor32.Pooler.Instance.Line(prevMouse, nextMouse, color, thickness, TextureColor32.mask);

                sprite.Blend(line, TextureColor32.alpha);
                sprite.mTexture.Apply();

                TextureColor32.Pooler.Instance.FreeTexture(line.mTexture);
                TextureColor32.Pooler.Instance.FreeSprite(line);
            }
        }
        else
        {
            dragging = false;
        }

        prevMouse = nextMouse;
    }
}
