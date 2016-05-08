using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WorldView worldView;

    
    [SerializeField] private Slider zoomSlider;

    private World world;

    private void Start()
    {
        world = new World
        {
            actors = new List<Actor>(),
        };

        for (int i = 0; i < 16; ++i)
        {
            var next = Vector2.right * Random.Range(-8, 8) * 32
                     + Vector2.up    * Random.Range(-8, 8) * 32;

            world.actors.Add(new Actor
            {
                world = world,
                position = new Position
                {
                    prev = next,
                    next = next,
                    progress = 0,
                },
            });
        }

        worldView.actors.SetActive(world.actors);
    }

    private static Vector2[] directions =
    {
        Vector2.up,
        Vector2.right,
        Vector2.down,
        Vector2.left,
    };

    private void CheckHotkeys()
    {
        var pan = Vector2.zero;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            pan += Vector2.right;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            pan += Vector2.left;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            pan += Vector2.up;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            pan += Vector2.down;
        }

        cameraController.focusTarget += pan * 64 * Time.deltaTime;
        cameraController.scaleTarget = zoomSlider.value * (Screen.width / 256);
    }

    private void Update()
    {
        CheckHotkeys();

        foreach (Actor actor in world.actors)
        {
            actor.position.progress += Time.deltaTime;

            while (actor.position.progress >= 1)
            {
                actor.position.prev = actor.position.next;
                actor.position.next = actor.position.prev + directions[Random.Range(0, 4)] * 32;
                actor.position.progress -= 1;
            }
        }
    }
}
