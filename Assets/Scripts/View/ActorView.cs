using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ActorView : MonoBehaviour 
{
    [SerializeField] private new SpriteRenderer renderer;

    public Actor actor { get; private set; }

    public void SetActor(Actor actor)
    {
        this.actor = actor;
    }

    public void Refresh()
    {
        transform.position = actor.position.current;
        renderer.sprite = actor.costume[actor.position.direction];
    }
}
