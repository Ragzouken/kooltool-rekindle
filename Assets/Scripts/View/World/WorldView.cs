using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using kooltool;

public class WorldView : InstanceView<Scene> 
{
    [SerializeField] private ImageGridView backgroundView;
    [SerializeField] private TileMapView tilemapView;   
    [SerializeField] private InstancePoolSetup actorSetup;

    public InstancePool<Actor> actors;

    private void Awake()
    {
        actors = actorSetup.Finalise<Actor>(sort: false);
    }

    protected override void Configure()
    {
        backgroundView.SetConfig(config.background);
        tilemapView.SetConfig(config.tilemap);

        Refresh();
    }

    public override void Refresh()
    {
        actors.SetActive(config.actors);
        actors.Refresh();

        backgroundView.Refresh();
        tilemapView.Refresh();
    }
}
