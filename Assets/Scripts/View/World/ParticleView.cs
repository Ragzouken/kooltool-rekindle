using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ParticleView : InstanceView<Trail.Particle> 
{
    [SerializeField] new private SpriteRenderer renderer;
    [SerializeField] private CycleHue cycle;

    protected override void Configure()
    {
        transform.position = config.position;
        cycle.period = Random.value;
    }

    public override void Refresh()
    {
        base.Refresh();

        int size = Mathf.FloorToInt(config.lifetime) + 1;
        renderer.sprite = Global.Instance.circles[size * 2];
        
        cycle.period = (config.offset + config.lifetime) % 1;
        cycle.Lightness = (int) (75 + 25 * Mathf.PingPong(config.lifetime, 1));
    }
}
