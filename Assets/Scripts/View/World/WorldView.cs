using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : InstanceView<World> 
{
    [SerializeField] private ImageGridView backgroundView;
    
    [SerializeField] private InstancePoolSetup actorSetup;

    public InstancePool<Actor> actors;

    private void Awake()
    {
        actors = actorSetup.Finalise<Actor>(sort: false);
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
