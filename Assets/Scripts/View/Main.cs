using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using InControl;

using UnityEngine.EventSystems;

public class Main : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WorldView worldView;

    [SerializeField] private Toggle moveToggle, takeToggle, makeToggle, killToggle;
    
    [SerializeField] private Slider zoomSlider;
    [SerializeField] private Sprite[] sprites;

    [SerializeField] private ToggleAnimatorBool toggler;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private SpriteRenderer testDraw;

    [SerializeField] private Image brightImage;
    [SerializeField] private Slider brightSlider;
    [SerializeField] private PalettePanel palettePanel;

    [SerializeField] private Material material1;
    [SerializeField] private Material material2;

    [Header("Test Drawing")]
    [SerializeField] private SpriteRenderer testDrawing;
    [SerializeField] private Texture2D testTexture;

    public World world { get; private set; }
    private World saved;

    private Actor possessed;

    public Texture2D test;

    public static bool mouseOverUI
    {
        get
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }

    private Script ScriptFromCSV(string csv)
    {
        string[] lines = csv.Split('\n');

        var fragments = new List<Fragment>();
        var fragment = new Fragment { name = "start" };
        var fraglines = new List<string[]>();

        for (int i = 0; i < lines.Length; ++i)
        {
            if (lines[i].Trim() == "") continue;

            string line = lines[i];

            string[] tokens = line.Split(',')
                                  .Select(token => token.Trim())
                                  .ToArray();

            if (tokens[0] == "script")
            {
                fragment.lines = fraglines.ToArray();
                fraglines.Clear();
                fragments.Add(fragment);

                fragment = new Fragment { name = tokens[1] };
            }
            else
            {
                fraglines.Add(tokens);
            }
        }

        fragment.lines = fraglines.ToArray();
        fragments.Add(fragment);

        return new Script
        {
            fragments = fragments.ToArray(),
        };
    }

    private void DebugScript(Script script)
    {
        var builder = new System.Text.StringBuilder("SCRIPT:\n");

        foreach (Fragment fragment in script.fragments)
        {
            builder.AppendFormat("[{0}]\n", fragment.name);
            builder.AppendFormat("{0}\n\n", string.Join("\n", fragment.lines.Select(line => string.Join(", ", line)).ToArray()));
        }

        Debug.Log(builder.ToString());
    }

    private Texture2D testTex;

    [System.Serializable]
    public class Stamp
    {
        public Sprite thumbnail;
        public Sprite brush;
    }

    [Header("Stamps")]
    public List<Stamp> stamps = new List<Stamp>();

    [SerializeField]
    private Transform stampParent;
    [SerializeField]
    private BrushToggle stampPrefab;

    private MonoBehaviourPooler<Stamp, BrushToggle> stampsp;


    private void Start()
    {
        testTex = BlankTexture.New(256, 256, Color.clear);
        testDraw.sprite = BlankTexture.FullSprite(testTex, pixelsPerUnit: 1);

        testTex.SetPixels32(testTexture.GetPixels32());
        testTex.Apply();

        test = BlankTexture.New(128, 32, Color.white);



        string path = Application.streamingAssetsPath + @"\test.txt";
        var script = ScriptFromCSV(File.ReadAllText(path));

        for (int i = 0; i < 4; ++i)
        {
            var rect = new Rect(32 * i, 0, 32, 32);

            PixelDraw.Brush.Apply(test, rect,
                                  sprites[i].texture, sprites[i].textureRect,
                                  PixelDraw.Blend.Replace);

            sprites[i] = Sprite.Create(test, rect, Vector2.one * 0.5f, 1);
        }

        test.Apply();

        stampsp = new MonoBehaviourPooler<Stamp, BrushToggle>(stampPrefab, stampParent, (s, i) => i.SetStamp(s));

        for (int i = 1; i < 8; ++i)
        {
            var stamp = new Stamp();

            //stamp.brush = PixelDraw.Brush.Circle(i, Color.white);
            stamp.brush = PixelDraw.Brush.Rectangle(i, i, Color.clear);

            for (int j = 0; j < i; ++j)
            {
                PixelDraw.IDrawingPaint.DrawCircle((PixelDraw.SpriteDrawing)stamp.brush,
                                                   new Vector2(Random.Range(0, i), Random.Range(0, i)),
                                                   1,
                                                   Color.white,
                                                   PixelDraw.Blend.Alpha);

            }

            stamp.thumbnail = PixelDraw.Brush.Rectangle(16, 16, Color.clear);
            PixelDraw.Brush.Apply(stamp.brush, Vector2.one * 8, stamp.thumbnail, Vector2.zero, PixelDraw.Blend.Alpha);

            stamp.brush.texture.Apply();
            stamp.thumbnail.texture.Apply();

            stamps.Add(stamp);
        }

        stampsp.SetActive(stamps);
        { stamp = stamps[0]; }

        var costume = new Costume
        {
            right = sprites[0],
            down = sprites[1],
            left = sprites[2],
            up = sprites[3],
        };

        var w = new World();
        
        for (int i = 1; i < 16; ++i)
        {
            w.palette[i] = new Color(Random.value, Random.value, Random.value, 1f);
        }

        SetWorld(w);

        for (int i = 0; i < 16; ++i)
        {
            var next = Vector2.right * Random.Range(-8, 8) * 32
                     + Vector2.up    * Random.Range(-8, 8) * 32;

            world.actors.Add(new Actor
            {
                world = world,
                costume = costume,
                script = script,
                state = new State { fragment = "start", line = 0 },
                position = new Position
                {
                    prev = next,
                    next = next + Vector2.right * 32,
                    progress = 0,
                },
            });
        }

        saved = world.Copy();

        Debug.LogFormat("Original:\n{0}", File.ReadAllText(path));

        var watcher = new FileSystemWatcher(Application.streamingAssetsPath);

        watcher.Changed += (source, args) =>
        {
            bool equal = string.Compare(Path.GetFullPath(path).TrimEnd('\\'),
                                        Path.GetFullPath(args.Name).TrimEnd('\\'), 
                                        true) == 0;

            if (equal)
            {
                var script2 = ScriptFromCSV(File.ReadAllText(path));
                DebugScript(script2);
            }
            else
            {
                Debug.LogFormat("boring, {0} changed", args.Name);
            }
        };

        watcher.EnableRaisingEvents = true;

        //Application.OpenURL(path);

        input = new TestInputSet();
    }

    private class TestInputSet : PlayerActionSet
    {
        public PlayerAction expand;
        public PlayerTwoAxisAction move;
        public PlayerTwoAxisAction cursor;
        public PlayerAction click;

        public PlayerOneAxisAction zoom;

        public TestInputSet()
        {
            expand = CreatePlayerAction("Expand");
            expand.AddDefaultBinding(Mouse.RightButton);
            expand.AddDefaultBinding(Key.Space);
            expand.AddDefaultBinding(InputControlType.Action4);

            {
                var up = CreatePlayerAction("Up");
                var down = CreatePlayerAction("Down");
                var left = CreatePlayerAction("Left");
                var right = CreatePlayerAction("Right");

                up.AddDefaultBinding(Key.W);
                down.AddDefaultBinding(Key.S);
                left.AddDefaultBinding(Key.A);
                right.AddDefaultBinding(Key.D);

                up.AddDefaultBinding(InputControlType.LeftStickUp);
                down.AddDefaultBinding(InputControlType.LeftStickDown);
                left.AddDefaultBinding(InputControlType.LeftStickLeft);
                right.AddDefaultBinding(InputControlType.LeftStickRight);

                move = CreateTwoAxisPlayerAction(left, right, down, up);
            }

            {
                var up = CreatePlayerAction("Cursor Up");
                var down = CreatePlayerAction("Cursor Down");
                var left = CreatePlayerAction("Cursor Left");
                var right = CreatePlayerAction("Cursor Right");

                up.AddDefaultBinding(InputControlType.RightStickUp);
                down.AddDefaultBinding(InputControlType.RightStickDown);
                left.AddDefaultBinding(InputControlType.RightStickLeft);
                right.AddDefaultBinding(InputControlType.RightStickRight);

                cursor = CreateTwoAxisPlayerAction(left, right, down, up);

                click = CreatePlayerAction("Cursor Click");
                click.AddDefaultBinding(InputControlType.RightTrigger);
            }

            {
                var zoomIn = CreatePlayerAction("Zoom In");
                var zoomOut = CreatePlayerAction("Zoom Out");

                zoomIn.AddDefaultBinding(InputControlType.LeftStickButton);
                zoomOut.AddDefaultBinding(InputControlType.RightStickButton);

                zoom = CreateOneAxisPlayerAction(zoomOut, zoomIn);
            }
        }
    }

    private TestInputSet input;

    private static Vector2[] directions =
    {
        Vector2.right,
        Vector2.down,
        Vector2.left,
        Vector2.up,
    };

    private Stack<System.Action> undos = new Stack<System.Action>();
    private Stack<Texture2D> copies = new Stack<Texture2D>();

    private void CheckHotkeys()
    {
        if (input.expand.WasPressed)
        {
            toggler.Toggle();
        }

        var pan = input.move.Value;

        Position.Direction direction = Position.Direction.Down;
        bool change = false;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            direction = Position.Direction.Right;
            change = true;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            direction = Position.Direction.Left;
            change = true;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            direction = Position.Direction.Up;
            change = true;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
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

        float mult = input.click.IsPressed ? 32 : 64;

        cursor.localPosition += (Vector3) input.cursor.Value * mult * Time.deltaTime;

        var system = EventSystem.current;
        var pointer = new PointerEventData(system);
        var raycasts = new List<RaycastResult>();

        pointer.position = new Vector3((cursor.localPosition.x / 256f + 0.5f) * Screen.width,
                                       (cursor.localPosition.y / 256f + 0.5f) * Screen.height);

        zoomSlider.value += input.zoom * 4 * Time.deltaTime;

        EventSystem.current.RaycastAll(pointer, raycasts);

        Debug.Log(pointer.position);

        if (raycasts.Count > 0)
        {
            if (hovering != raycasts[0].gameObject)
            {
                if (hovering != null)
                {
                    ExecuteEvents.ExecuteHierarchy(hovering, pointer, ExecuteEvents.pointerExitHandler);
                }

                ExecuteEvents.ExecuteHierarchy(raycasts[0].gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            }

            hovering = raycasts[0].gameObject;
        } 
        else if (hovering != null)
        {
            ExecuteEvents.ExecuteHierarchy(hovering, pointer, ExecuteEvents.pointerExitHandler);

            hovering = null;
        }

        if (input.click.WasPressed && raycasts.Count > 0)
        {
            ExecuteEvents.ExecuteHierarchy(raycasts[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(raycasts[0].gameObject, pointer, ExecuteEvents.beginDragHandler);

            dragging = raycasts[0].gameObject;
        }

        if (input.click.IsPressed && dragging != null)
        {
            ExecuteEvents.ExecuteHierarchy(dragging, pointer, ExecuteEvents.dragHandler);
        }

        if (input.click.WasReleased && raycasts.Count > 0)
        {
            ExecuteEvents.ExecuteHierarchy(raycasts[0].gameObject, pointer, ExecuteEvents.pointerUpHandler);

            if (dragging != null) ExecuteEvents.ExecuteHierarchy(dragging, pointer, ExecuteEvents.endDragHandler);
            if (dragging == raycasts[0].gameObject) ExecuteEvents.ExecuteHierarchy(dragging, pointer, ExecuteEvents.pointerClickHandler);
        }

        if (undos.Count > 0
         && Input.GetKeyDown(KeyCode.Z))
        {
            undos.Pop()();
        }
    }

    private void SetWorld(World world)
    {
        this.world = world;

        palettePanel.SetWorld(world);

        for (int i = 0; i < 16; ++i)
        {
            RefreshPalette(i);
        }
    }

    private Stamp stamp;

    public void SetStamp(Stamp stamp)
    {
        this.stamp = stamp;
    }

    public void EditPalette(int i, Color color)
    {
        world.palette[i] = color;

        RefreshPalette(i);
    }

    public void RecordPaletteHistory(int i, Color prev, Color next)
    {
        undos.Push(() => EditPalette(i, prev));
    }

    private void RefreshPalette(int i)
    {
        string name = string.Format("_Palette{0:D2}", i);

        material1.SetColor(name, world.palette[i]);
        material2.SetColor(name, world.palette[i]);
    }

    private GameObject hovering;
    private GameObject dragging;

    private bool clickedOnWorld;
    private bool clickingOnWorld;

    private Vector2 nextCursor, nextMouse;
    private Vector2 prevCursor, prevMouse;

    private void Update()
    {
        var color = Color.white * brightSlider.value;
        color.a = 1;

        brightImage.color = color;

        nextCursor = new Vector2((cursor.localPosition.x / 256f + 0.5f) * Screen.width,
                                 (cursor.localPosition.y / 256f + 0.5f) * Screen.height);

        worldView.actors.SetActive(world.actors);

        CheckHotkeys();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (Actor actor in world.actors)
            {
                actor.state.fragment = "bump";
                actor.state.line = 0;
            }
        }

        float interval = 0.1f;

        world.timer += Time.deltaTime;

        while (world.timer > interval)
        {
            world.timer -= interval;

            foreach (Actor actor in world.actors)
            {
                if (actor.position.moving) continue;

                while (true)
                {
                    var fragment = actor.script.fragments.FirstOrDefault(frag => frag.name == actor.state.fragment);

                    if (fragment != null
                     && actor.state.line < fragment.lines.Length)
                    {
                        string[] line = fragment.lines[actor.state.line];

                        if (line[0] == "move")
                        {
                            int dir = 0;

                            if (line[1] == "random") dir = Random.Range(0, 4);
                            if (line[1] == "east") dir = 0;
                            if (line[1] == "south") dir = 1;
                            if (line[1] == "west") dir = 2;
                            if (line[1] == "north") dir = 3;

                            actor.position.next = actor.position.prev + directions[dir] * 32;
                            actor.position.direction = (Position.Direction)dir;
                        }
                        else if (line[0] == "follow")
                        {
                            actor.state.fragment = line[1];
                            actor.state.line = 0;
                            continue;
                        }

                        actor.state.line += 1;
                        break;
                    }
                }
            }
        }

        foreach (Actor actor in world.actors)
        {
            if (!actor.position.moving) continue;

            var delta = actor.position.next - actor.position.prev + Vector2.one * 0.5f;

            //actor.position.direction = (Position.Direction)Mathf.FloorToInt(Mathf.Atan2(delta.y, -delta.x) / (Mathf.PI * 0.5f) + 2);

            actor.position.progress += Time.deltaTime * 2;

            if (actor.position.progress >= 1)
            {
                actor.position.prev = actor.position.next;
                actor.position.progress = 0;
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
            SetWorld(saved.Copy());
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
            var pos = new Vector2((cursor.localPosition.x / 256f + 0.5f) * Screen.width,
                                  (cursor.localPosition.y / 256f + 0.5f) * Screen.height);

            ray = Camera.main.ScreenPointToRay(pos);
            plane.Raycast(ray, out t);
            point = ray.GetPoint(t);

            Vector2 delta = point - cameraController.focusTarget;

            //cameraController.focusTarget += delta * 2f * Time.deltaTime;

            if (Physics.Raycast(ray, out hit))
            {
                Actor actor = hit.collider.GetComponent<ActorView>().actor;

                Vector2 local = point - actor.position.current - Vector2.one * 16;

                if (input.click.IsPressed)
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

        //nextCursor = (Vector2.one * 128 + input.cursor.Value * 32) * 3;

        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            plane.Raycast(ray, out t);
            nextMouse = ray.GetPoint(t);

            var cursorScreen = new Vector2(cursor.anchoredPosition.x / 256,
                                           cursor.anchoredPosition.y / 256);

            cursorScreen.Scale(Vector2.one * Screen.width);

            nextCursor = cursorScreen;
            ray = Camera.main.ScreenPointToRay(cursorScreen);
            plane.Raycast(ray, out t);
            nextCursor = ray.GetPoint(t);
        }

        bool mouse = Input.GetMouseButton(0);
        bool gamep = input.click.IsPressed;

        Vector2 prev = gamep ? prevCursor : prevMouse;
        Vector2 next = gamep ? nextCursor : nextMouse;

        if (mouse || gamep)
        {
            if (!dragging_)
            {
                dragging_ = true;

                var prevt = new Texture2D(256, 256);
                prevt.SetPixels32(testTex.GetPixels32());

                undos.Push(() =>
                {
                    testTex.SetPixels32(prevt.GetPixels32());
                    testTex.Apply();
                });
            }
            else
            {
                var sprite = testDraw.sprite;

                var adj = new Color(palettePanel.selected / 15f, 0, 0);

                PixelDraw.Blend.BlendFunction blend = delegate (Color canvas, Color brush)
                {
                    return canvas * (1 - brush.a) + adj * brush.a;
                };

                PixelDraw.IDrawingPaint.SweepBrush((PixelDraw.SpriteDrawing)sprite,
                                                   prev,
                                                   next,
                                                   stamp.brush,
                                                   blend);

                sprite.texture.Apply();
            }
        }
        else
        {
            dragging_ = false;
        }

        prevMouse = nextMouse;
        prevCursor = nextCursor;
    }

    private bool dragging_;
}
