using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class WorldView : InstanceView<World> 
{
    [SerializeField] private Transform belowTileParent;
    [SerializeField] private Transform aboveTileParent;

    [SerializeField] private ImageGridView backgroundView;

    [SerializeField] private InstancePoolSetup actorSetup;

    public InstancePool<Actor> actors;

    [SerializeField] private InstancePoolSetup testSetup;
    public AnonymousPool<Color, SpriteRenderer> test;

    private void Awake()
    {
        actors = actorSetup.Finalise<Actor>(sort: false);

        test = testSetup.Finalise<Color, SpriteRenderer>((c, r) => r.color = c);

        test.SetActive(Enumerable.Range(0, 5).Select(i => Color.red * Random.value));
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
