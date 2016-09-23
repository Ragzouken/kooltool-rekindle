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

    public PoolerPro<Actor, ActorView> actors;

    private void Awake()
    {
        actors = new PoolerPro<Actor, ActorView>(spritePrefab, actorParent);
    }

    public void Setup(World world)
    {
        model = world;
        backgroundView.Setup(world.background);
    }

    private void Update()
    {
        actors.Refresh();

        backgroundView.Refresh();
    }
}
