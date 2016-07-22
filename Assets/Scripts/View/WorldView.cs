using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : ViewComponent<World> 
{
    [SerializeField] private ActorView spritePrefab;

    [SerializeField] private Transform belowTileParent;
    [SerializeField] private Transform aboveTileParent;
    [SerializeField] private Transform actorParent;

    [SerializeField] private ImageGridView backgroundView;

    public MonoBehaviourPooler<Actor, ActorView> actors;

    private void Awake()
    {
        actors = new MonoBehaviourPooler<Actor, ActorView>(spritePrefab,
                                                           actorParent,
                                                           (a, r) => r.SetActor(a));
    }

    public void Setup(World world)
    {
        model = world;
        backgroundView.Setup(world.background);
    }

    private void Update()
    {
        actors.MapActive((actor, render) => render.Refresh());

        backgroundView.Refresh();
    }
}
