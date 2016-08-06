using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


public class TextureDebugger : MonoBehaviour
{
    [SerializeField]
    private RectTransform texParent;
    [SerializeField]
    private RawImage texPrefab;

    private MonoBehaviourPooler<DrawingTexture, RawImage> textures;

    private void Awake()
    {
        textures = new MonoBehaviourPooler<DrawingTexture, RawImage>(texPrefab, texParent, (t, r) => r.texture = t.texture);
    }

    private void Update()
    {
        foreach (var t in DrawingTexturePooler.debugs) t.Apply();
        textures.SetActive(DrawingTexturePooler.debugs);
    }
}
