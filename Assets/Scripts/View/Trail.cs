using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Trail : MonoBehaviour 
{
    private class Particle
    {
        public Vector3 position;
        public float lifetime;
        public float offset;
    }

    public CameraController cam;

    [SerializeField] private Transform particleParent;
    [SerializeField] private SpriteRenderer particlePrefab;

    private MonoBehaviourPooler<Particle, SpriteRenderer> renderers;

    private List<Particle> particles = new List<Particle>();

    private Vector2 prev;
    private Vector2 next;

    private void Awake()
    {
        renderers = new MonoBehaviourPooler<Particle, SpriteRenderer>(particlePrefab,
                                                                      particleParent,
                                                                      (p, r) => 
                                                                      {
                                                                          r.transform.position = p.position;
                                                                          r.GetComponent<CycleHue>().period = Random.value;
                                                                      } );

        prev = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void Update()
    {
        foreach (Particle particle in particles)
        {
            particle.lifetime -= Time.deltaTime * 10;
        }

        particles.RemoveAll(p => p.lifetime <= 0);
        renderers.SetActive(particles);
        renderers.MapActive((p, r) =>
        {
            int size = Mathf.FloorToInt(p.lifetime) + 1;
            r.sprite = Global.Instance.circles[size * 2];

            var cycle = r.GetComponent<CycleHue>();

            cycle.period = (p.offset + p.lifetime) % 1;
            cycle.Lightness = (int) (75 + 25 * Mathf.PingPong(p.lifetime, 1));
        });

        float angle = ((Time.timeSinceLevelLoad * 2) % 1) * Mathf.PI * 2;

        Vector3 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        next = cam.ScreenToWorld(Input.mousePosition) + offset * 4;

        foreach (var point in PixelDraw.Bresenham.Line((int)prev.x, (int)prev.y, (int)next.x, (int)next.y))
        {
            particles.Add(new Particle
            {
                position = point,
                lifetime = 4,
                offset = Random.value,
            });
        }

        prev = next;
    }
}
