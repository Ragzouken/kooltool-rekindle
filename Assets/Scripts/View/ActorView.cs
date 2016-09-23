using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ActorView : PoolerInstance<Actor>
{
    [SerializeField] private new SpriteRenderer renderer;

    public override void Refresh()
    {
        transform.position = shortcut.position.current;
        renderer.sprite = shortcut.costume[shortcut.position.direction];
        renderer.sortingLayerName = "World - Actors";
    }
}
