using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : MonoBehaviour 
{
    [SerializeField] private SpriteRenderer spritePrefab;

    [SerializeField] private Transform belowTileParent;
    [SerializeField] private Transform aboveTileParent;
    [SerializeField] private Transform actorParent;

    public MonoBehaviourPooler<Actor, SpriteRenderer> actors;

    private void Awake()
    {
        actors = new MonoBehaviourPooler<Actor, SpriteRenderer>(spritePrefab,
                                                                actorParent);
    }

    private void Update()
    {
        actors.MapActive((actor, render) =>
        {
            render.transform.position = actor.position.current;
        });
    }
}
