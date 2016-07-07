using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Voronoi : MonoBehaviour 
{
    [SerializeField] private MeshRenderer conePrefab;
    [SerializeField] private Transform coneParent;
    [SerializeField] private Color[] palette;

    private MonoBehaviourPooler<Color, MeshRenderer> colors;

    private void Awake()
    {
        colors = new MonoBehaviourPooler<Color, MeshRenderer>(conePrefab,
                                                              coneParent,
                                                              InitCone);

        palette = new Color[15];

        for (int i = 0; i < 15; ++i) palette[i] = new Color(Random.value, Random.value, Random.value, 1f);

        colors.SetActive(palette);
    }

    private void InitCone(Color color, MeshRenderer renderer)
    {
        float h, s, v;

        Color.RGBToHSV(color, out h, out s, out v);

        renderer.transform.localPosition = new Vector3(h - 0.5f, 0, v - 0.5f);
        renderer.material.color = color;
    }

    private void Update()
    {
        colors.SetActive(palette);
    }
}
