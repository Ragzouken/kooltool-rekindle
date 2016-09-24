using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : InstanceView<World> 
{
    [SerializeField] private ActorView actorPrefab;

    [SerializeField] private Transform belowTileParent;
    [SerializeField] private Transform aboveTileParent;
    [SerializeField] private Transform actorParent;

    [SerializeField] private ImageGridView backgroundView;

    public InstancePool<Actor, ActorView> actors;

    private void Awake()
    {
        actors = new InstancePool<Actor, ActorView>(actorPrefab, actorParent);
    }

    protected override void Configure()
    {
        backgroundView.SetConfig(config.background);
    }

    public override void Refresh()
    {
        base.Refresh();

        actors.SetActive(config.actors);
        actors.Refresh();

        backgroundView.Refresh();
    }
}
