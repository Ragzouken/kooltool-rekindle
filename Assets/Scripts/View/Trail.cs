using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Trail : MonoBehaviour 
{
    public class Particle
    {
        public Vector3 position;
        public float lifetime;
        public float offset;
    }

    public CameraController cam;

    [SerializeField] private InstancePoolSetup particlesSetup;

    private InstancePool<Particle> particles_;

    private List<Particle> particles = new List<Particle>();

    private Vector2 prev;
    public Vector2 next;

    private void Awake()
    {
        particles_ = particlesSetup.Finalise<Particle>(sort: false);

        prev = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private Vector2 Border(float u)
    {
        if (u <= 0.25f) return Vector2.Lerp(new Vector2( 1,  1), new Vector2( 1, -1), (u - 0.00f) * 4);
        if (u <= 0.50f) return Vector2.Lerp(new Vector2( 1, -1), new Vector2(-1, -1), (u - 0.25f) * 4);
        if (u <= 0.75f) return Vector2.Lerp(new Vector2(-1, -1), new Vector2(-1,  1), (u - 0.50f) * 4);
        if (u <= 1.00f) return Vector2.Lerp(new Vector2(-1,  1), new Vector2( 1,  1), (u - 0.75f) * 4);

        return Vector2.zero;
    }

    private void Update()
    {
        foreach (Particle particle in particles)
        {
            particle.lifetime -= Time.deltaTime * 10;
        }

        particles.RemoveAll(p => p.lifetime <= 0);
        particles_.SetActive(particles);
        particles_.Refresh();

        float u = ((Time.timeSinceLevelLoad * 2) % 1);
        float angle = u * Mathf.PI * 2;

        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        //offset = Border(u);

        //next = transform.position; //cam.focusTarget + offset * 8;

        Bresenham.Line((int) prev.x, (int) prev.y, (int) next.x, (int) next.y, (x, y) =>
        {
            this.particles.Add(new Particle
            {
                position = new Vector2(x, y),
                lifetime = 4,
                offset = Random.value,
            });
        });

        prev = next;
    }
}
