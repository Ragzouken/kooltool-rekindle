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

        offset = Random.value;
        block = new MaterialPropertyBlock();
    }

    private MaterialPropertyBlock block;
    private float offset;

    public void Refresh()
    {
        transform.position = actor.position.current;
        renderer.sprite = actor.costume[actor.position.direction];

        renderer.GetPropertyBlock(block);
        block.SetFloat("_Cutout", (Time.timeSinceLevelLoad + offset) % 1);
        renderer.SetPropertyBlock(block);
    }
}
