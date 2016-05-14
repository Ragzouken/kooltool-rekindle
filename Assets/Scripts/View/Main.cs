﻿using UnityEngine;
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
    [SerializeField] private Sprite[] sprites;

    private World world;
    private World saved;

    private Actor possessed;

    public Texture2D test;

    public static bool mouseOverUI
    {
        get
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }

    private void Start()
    {
        test = BlankTexture.New(128, 32, Color.white);

        for (int i = 0; i < 4; ++i)
        {
            var rect = new Rect(32 * i, 0, 32, 32);

            PixelDraw.Brush.Apply(test, rect,
                                  sprites[i].texture, sprites[i].textureRect,
                                  PixelDraw.Blend.Replace);

            sprites[i] = Sprite.Create(test, rect, Vector2.one * 0.5f, 1);
        }

        test.Apply();

        var costume = new Costume
        {
            right = sprites[0],
            down = sprites[1],
            left = sprites[2],
            up = sprites[3],
        };

        world = new World();

        for (int i = 0; i < 16; ++i)
        {
            var next = Vector2.right * Random.Range(-8, 8) * 32
                     + Vector2.up    * Random.Range(-8, 8) * 32;

            world.actors.Add(new Actor
            {
                world = world,
                costume = costume,
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
        Vector2.right,
        Vector2.down,
        Vector2.left,
        Vector2.up,
    };

    private void CheckHotkeys()
    {
        var pan = Vector2.zero;

        Position.Direction direction = Position.Direction.Down;
        bool change = false;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            pan += Vector2.right;
            direction = Position.Direction.Right;
            change = true;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            pan += Vector2.left;
            direction = Position.Direction.Left;
            change = true;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            pan += Vector2.up;
            direction = Position.Direction.Up;
            change = true;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            pan += Vector2.down;
            direction = Position.Direction.Down;
            change = true;
        }

        if (possessed != null)
        {
            cameraController.focusTarget = worldView.actors.Get(possessed).transform.localPosition;

            if (!possessed.position.moving)
            {
                possessed.position.next = possessed.position.prev + pan * 32;
                if (change) possessed.position.direction = direction;
            }

            pan = Vector2.zero;
        }

        cameraController.focusTarget += pan * 64 * Time.deltaTime;
        cameraController.scaleTarget = zoomSlider.value * (Screen.width / 256);
    }

    private bool clickedOnWorld;
    private bool clickingOnWorld;
    private Vector2 prevMouse;

    private void Update()
    {
        worldView.actors.SetActive(world.actors);

        CheckHotkeys();

        foreach (Actor actor in world.actors)
        {
            if (!actor.position.moving) continue;

            var delta = actor.position.next - actor.position.prev + Vector2.one * 0.5f;

            //actor.position.direction = (Position.Direction)Mathf.FloorToInt(Mathf.Atan2(delta.y, -delta.x) / (Mathf.PI * 0.5f) + 2);

            actor.position.progress += Time.deltaTime * 2;

            if (actor == possessed && actor.position.progress >= 1)
            {
                actor.position.prev = actor.position.next;
                actor.position.progress = 0;
            }

            while (actor.position.progress >= 1)
            {
                int dir = Random.Range(0, 4);

                actor.position.prev = actor.position.next;
                actor.position.next = actor.position.prev + directions[dir] * 32;
                actor.position.direction = (Position.Direction) dir;
                actor.position.progress -= 1;
            }
        }

        clickedOnWorld = !mouseOverUI && Input.GetMouseButtonDown(0);

        clickingOnWorld = clickedOnWorld
                       || (clickingOnWorld && Input.GetMouseButton(0));

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            saved = world.Copy();
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket) && saved != null)
        {
            world = saved.Copy();
        }

        var plane = new Plane(Vector3.forward, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float t;
        plane.Raycast(ray, out t);
        Vector2 point = ray.GetPoint(t);
        RaycastHit hit;

        if (clickedOnWorld)
        { 
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
            }
        }

        if (moveToggle.isOn)
        {
            Vector2 delta = point - cameraController.focusTarget;

            //cameraController.focusTarget += delta * 2f * Time.deltaTime;

            if (Physics.Raycast(ray, out hit))
            {
                Actor actor = hit.collider.GetComponent<ActorView>().actor;

                Vector2 local = point - actor.position.current - Vector2.one * 16;

                if (clickedOnWorld)
                {
                    using (var brush = PixelDraw.Brush.Line(prevMouse, point, Color.red, 1))
                    //using (var brush = PixelDraw.Brush.Circle(3, Color.red))
                    {
                        var sprite = actor.costume[actor.position.direction];

                        Vector2 current = actor.position.current;
                        

                        local.x = Mathf.Round(local.x);
                        local.y = Mathf.Round(local.y);

                        //PixelDraw.Brush.Apply(brush, prevMouse, sprite, Vector2.zero, PixelDraw.Blend.Alpha);

                        PixelDraw.IDrawingPaint.DrawLine((PixelDraw.SpriteDrawing)sprite,
                                                         prevMouse - actor.position.current,
                                                         local,
                                                         1,
                                                         Color.red,
                                                         PixelDraw.Blend.Alpha);

                        //PixelDraw.Brush.Line(prevMouse, point, Color.red, 1);

                        sprite.texture.Apply();
                    }
                }
            }
        }

        prevMouse = point;
    }
}
