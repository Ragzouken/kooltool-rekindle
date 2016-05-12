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

    [SerializeField] private Toggle moveToggle, takeToggle, makeToggle, killToggle;
    
    [SerializeField] private Slider zoomSlider;

    private World world;
    private World saved;

    private Actor possessed;

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
                    next = next + Vector2.right * 32,
                    progress = 0,
                },
            });
        }

        saved = world.Copy();
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

        if (possessed != null)
        {
            cameraController.focusTarget = worldView.actors.Get(possessed).transform.localPosition;

            if (!possessed.position.moving)
            {
                possessed.position.next = possessed.position.prev + pan * 32;
            }

            pan = Vector2.zero;
        }

        cameraController.focusTarget += pan * 64 * Time.deltaTime;
        cameraController.scaleTarget = zoomSlider.value * (Screen.width / 256);
    }

    private void Update()
    {
        worldView.actors.SetActive(world.actors);

        CheckHotkeys();

        foreach (Actor actor in world.actors)
        {
            if (!actor.position.moving) continue;

            actor.position.progress += Time.deltaTime;

            if (actor == possessed && actor.position.progress >= 1)
            {
                actor.position.prev = actor.position.next;
                actor.position.progress = 0;
            }

            while (actor.position.progress >= 1)
            {
                actor.position.prev = actor.position.next;
                actor.position.next = actor.position.prev + directions[Random.Range(0, 4)] * 32;
                actor.position.progress -= 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            saved = world.Copy();
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && saved != null)
        {
            world = saved.Copy();
        }

        if (Input.GetMouseButtonDown(0))
        {
            var plane = new Plane(Vector3.forward, Vector3.zero);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float t;
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Actor actor = hit.collider.GetComponent<ActorView>().actor;

                if (takeToggle.isOn)
                {
                    possessed = possessed == actor ? null : actor;
                }
                else if (killToggle.isOn)
                {
                    if (possessed == actor) possessed = null;

                    world.actors.Remove(actor);
                }
            }
            else if (plane.Raycast(ray, out t))
            {
                Vector3 point = ray.GetPoint(t);

                if (makeToggle.isOn)
                {
                    point.x = Mathf.RoundToInt(point.x / 32);
                    point.y = Mathf.RoundToInt(point.y / 32);

                    world.actors.Add(new Actor
                    {
                        position = new Position { next = point * 32, prev = point * 32 },
                        world = world,
                    });
                }
                else if (moveToggle.isOn)
                {
                    cameraController.focusTarget = point;
                }
            }
        }
    }
}
