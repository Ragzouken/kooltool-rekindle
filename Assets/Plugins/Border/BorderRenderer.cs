using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace PixelBorder
{
    [ExecuteInEditMode]
    public class BorderRenderer : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private RawImage image;

        public Sprite sourceSprite;

        private RenderTexture texture;

        private const int size = 1;

        private void Awake()
        {
            texture = new RenderTexture(36, 36, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.Create();

            image.texture = texture;
        }
        
        private void Update()
        {
            //return;
        
            if (!material.shader.isSupported)
            {
                image.enabled = false;

                return;
            }

            if (sourceSprite != null
             && image        != null
             && material     != null) Render();
        }

        [ContextMenu("Render")]
        public void Render()
        {
            Sprite sprite = sourceSprite;
            Rect rect = sprite.textureRect;

            float iw = 1f / sprite.texture.width;
            float ih = 1f / sprite.texture.height;

            var srcPixel = new Vector2(iw, ih);
            var dstPixel = new Vector2(1f / texture.width, 1f / texture.height);

            material.SetVector("_SpriteUV",
                               new Vector4(rect.xMin * iw,
                                           rect.yMin * ih,
                                           rect.xMax * iw,
                                           rect.yMax * ih));

            material.SetVector("_DstPixel", dstPixel);
            material.SetVector("_SrcPixel", srcPixel);
            material.SetVector("_Scale", new Vector2(texture.width  / rect.width,
                                                     texture.height / rect.height));

            image.uvRect = Rect.MinMaxRect(0, 
                                           0, 
                                           (rect.width  + size * 2) / texture.width, 
                                           (rect.height + size * 2) / texture.height);
            image.SetNativeSize();

            Graphics.Blit(sprite.texture, texture, material);
        }
    }
}
