using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : MonoBehaviour 
{
    [SerializeField] private ActorView spritePrefab;

    [SerializeField] private Transform belowTileParent;
    [SerializeField] private Transform aboveTileParent;
    [SerializeField] private Transform actorParent;

    public MonoBehaviourPooler<Actor, ActorView> actors;

    private void Awake()
    {
        actors = new MonoBehaviourPooler<Actor, ActorView>(spritePrefab,
                                                           actorParent,
                                                           (a, r) => r.SetActor(a));
    }

    private void Update()
    {
        actors.MapActive((actor, render) => render.Refresh());
    }
}
