using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ActorView : InstanceView<Actor>
{
    [SerializeField] private new SpriteRenderer renderer;

    public override void Refresh()
    {
        transform.position = config.position.current;
        renderer.sprite = config.costume[config.position.direction].uSprite;
    }
}
