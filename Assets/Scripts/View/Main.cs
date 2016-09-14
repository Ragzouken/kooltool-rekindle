using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using InControl;

using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Stopwatch = System.Diagnostics.Stopwatch;

public class Main : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Delete Prefs")]
    public static void DeletePrefs()
    {
        PlayerPrefs.DeleteAll();
    }
#endif

    [SerializeField] private CameraController cameraController;
    [SerializeField] private WorldView worldView;

    [SerializeField] private Toggle freeToggle, stampToggle;

    [SerializeField] private Slider zoomSlider;
    [SerializeField] private Texture2D costumeTexture;
    [SerializeField] private ManagedSprite<byte>[] sprites;

    [SerializeField] private RectTransform cursor;

    [SerializeField] private Image brightImage;
    [SerializeField] private Slider brightSlider;
    [SerializeField] private PalettePanel palettePanel;

    [SerializeField] private Material material1;
    [SerializeField] private Material material2;
    [SerializeField] private GameObject saveBlocker;

    [SerializeField] private RectTransform mouseCursorTransform;
    [SerializeField] private Image mouseCursorImage;
    [SerializeField] private Sprite normalCursor;
    [SerializeField] private Sprite pickCursor, stampCursor;

    public Project project { get; private set; }
    private World saved;

    private Actor possessed;

    private TextureByte test;

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
        public ManagedSprite<byte> brush;
    }

    [Header("Stamps")]
    public List<Stamp> stamps = new List<Stamp>();

    [SerializeField]
    private Transform stampParent;
    [SerializeField]
    private BrushToggle stampPrefab;

    private MonoBehaviourPooler<Stamp, BrushToggle> stampsp;

    public Sprite[] testbrushes;
    private ManagedSprite<byte> brushSpriteD;

    private void Start()
    { 
        freeToggle.isOn = true;

        Cursor.visible = false;

        {
            test = new TextureByte(128, 128);
            test.Clear(0);

            var pixels = costumeTexture.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
            {
                byte value = 0;

                if (pixels[i] == Color.white) value = 1;
                if (pixels[i] == Color.black) value = 2;

                test.pixels[i] = value;
            }

            test.Apply();
            test.uTexture.name = "Costume Texture";
        }

        brushSpriteD = new TextureByte(16, 16).FullSprite(IntVector2.one * 8);

        string path = Application.streamingAssetsPath + @"\test.txt";
        var script = ScriptFromCSV(File.ReadAllText(path));

        sprites = new ManagedSprite<byte>[4];

        for (int i = 0; i < 4; ++i)
        {
            var rect = new Rect(0, 32 * (3 - i), 32, 32);

            sprites[i] = new ManagedSprite<byte>(test, rect, IntVector2.one * 16);
            sprites[i].uSprite.name = "Costume " + i;
        }

        stampsp = new MonoBehaviourPooler<Stamp, BrushToggle>(stampPrefab, stampParent, (s, i) => i.SetStamp(s));

        foreach (var sprite in testbrushes)
        {
            int width  = (int) sprite.rect.width;
            int height = (int) sprite.rect.height;

            var tex = new TextureByte(width, height);
            tex.SetPixels(sprite.GetPixels().Select(c => ((Color32) c).a).ToArray());
            tex.Apply();

            stamps.Add(new Stamp
            {
                brush = tex.FullSprite(sprite.pivot),
                thumbnail = sprite,
            });
        }

        stampsp.SetActive(stamps);

        SetStamp(stamps[0]);

        palettePanel.OnPaletteIndexSelected += i => RefreshBrushCursor();

        var res = new TextureResource(test);

        var costume = new Costume
        {
            right = new SpriteResource(res, sprites[0]),
            down = new SpriteResource(res, sprites[1]),
            left = new SpriteResource(res, sprites[2]),
            up = new SpriteResource(res, sprites[3]),
        };

        var p = new Project();
        var w = new World();
        res.dirty = true;
        p.resources.Add(res);
        p.resources.Add(costume.right);
        p.resources.Add(costume.left);
        p.resources.Add(costume.down);
        p.resources.Add(costume.up);

        for (int i = 1; i < 16; ++i)
        {
            w.palette[i] = new Color(Random.value, Random.value, Random.value, 1f);
        }

        p.world = w;
        w.background.project = p;
        w.background.cellSize = 256;
        SetProject(p);

        for (int i = 0; i < 16; ++i)
        {
            var next = Vector2.right * Random.Range(-4, 4) * 32
                     + Vector2.up    * Random.Range(-4, 4) * 32;

            project.world.actors.Add(new Actor
            {
                world = project.world,
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

        saved = project.world.Copy();

        //Debug.LogFormat("Original:\n{0}", File.ReadAllText(path));

        ///*
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
        //*/

        //Application.OpenURL(path);

        input = new TestInputSet();

#if UNITY_WEBGL
        Debug.Log("Location: " + GetWindowSearch());

        try
        {
            string id = GetWindowSearch().Split('=')[1];

            FromGist(id);

        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
#endif
    }

    public class TestInputSet : PlayerActionSet
    {
        public PlayerAction confirm;
        public PlayerAction cancel;

        public PlayerAction expand;
        public PlayerTwoAxisAction move;
        public PlayerTwoAxisAction cursor;
        public PlayerAction click;

        public PlayerOneAxisAction zoom;

        public TestInputSet()
        {
            expand = CreatePlayerAction("Expand");
            //expand.AddDefaultBinding(Mouse.RightButton);
            //expand.AddDefaultBinding(Key.Space);
            //expand.AddDefaultBinding(InputControlType.Action4);

            cancel = CreatePlayerAction("Cancel");
            cancel.AddDefaultBinding(Mouse.RightButton);
            cancel.AddDefaultBinding(Key.Escape);
            cancel.AddDefaultBinding(InputControlType.Action2);

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

    public TestInputSet input;

    private static Vector2[] directions =
    {
        Vector2.right,
        Vector2.down,
        Vector2.left,
        Vector2.up,
    };

    private Stack<Changes> undos = new Stack<Changes>();
    private Stack<Changes> redos = new Stack<Changes>();

    private void Do(Changes change)
    {
        redos.Clear();
        undos.Push(change);
    }

    private void Undo()
    {
        if (undos.Count == 0) return;

        var change = undos.Pop();

        change.Undo();

        redos.Push(change);
    }

    private void Redo()
    {
        if (redos.Count == 0) return;

        var change = redos.Pop();

        change.Redo();

        undos.Push(change);
    }

    private List<RaycastResult> raycasts = new List<RaycastResult>();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void UpdateGistID(string id);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string GetGistID();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string GetWindowSearch();

    private void FromGist(string id)
    {
        StartCoroutine(Gist.Download(id, dict =>
        {
            project.world.palette = BytesToColors(FromBase64(dict["palette"]));

            dict.Remove("palette");

            foreach (var pair in dict)
            {
                string[] coords = pair.Key.Split(',');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                byte[] data = System.Convert.FromBase64String(pair.Value);

                var c = project.world.background.AddCell(new IntVector2(x, y));
                c.texture.texture8.DecodeFromPNG(data);
            }

            SetProject(project);
        }));
    }

    private string ToBase64(byte[] bytes)
    {
        return System.Convert.ToBase64String(bytes);
    }

    private byte[] FromBase64(string data)
    {
        return System.Convert.FromBase64String(data);
    }

    private byte[] ColorsToBytes(Color[] colors)
    {
        var bytes = new byte[colors.Length * 4];

        for (int i = 0; i < colors.Length; ++i)
        {
            var color = (Color32) colors[i];

            bytes[i * 4 + 0] = color.a;
            bytes[i * 4 + 1] = color.r;
            bytes[i * 4 + 2] = color.g;
            bytes[i * 4 + 3] = color.b;
        }

        return bytes;
    }

    private Color[] BytesToColors(byte[] bytes)
    {
        var colors = new Color[bytes.Length / 4];

        for (int i = 0; i < colors.Length; ++i)
        {
            var color = new Color32(bytes[i * 4 + 1],
                                    bytes[i * 4 + 2],
                                    bytes[i * 4 + 3],
                                    bytes[i * 4 + 0]);

            colors[i] = color;
        }

        return colors;
    }

    public void GISTSAVE()
    {
        var background = project.world.background.cells.ToDictionary(p => string.Format("{0},{1}", p.Key.x, p.Key.y),
                                                                     p => ToBase64(p.Value.texture.uTexture.EncodeToPNG()));

        background["palette"] = ToBase64(ColorsToBytes(project.world.palette));

        StartCoroutine(Gist.Create("test gist",
            background,
            id =>
            {
                Debug.Log(id);

#if UNITY_WEBGL
                    UpdateGistID(id);
#endif
            }));
    }

    private void CheckHotkeys()
    {
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

        ///*
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            StartCoroutine(LoadProject());
        }
        //*/
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            StartCoroutine(SaveProject());
        }
        //*/

        if (Input.GetKeyDown(KeyCode.Slash))
        {
            //PerfTest();
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
        raycasts.Clear();

        var temp = pointer.position;
        temp.x = (cursor.localPosition.x / 256f + 0.5f) * Screen.width;
        temp.y = (cursor.localPosition.y / 256f + 0.5f) * Screen.height;
        pointer.position = temp;

        zoomSlider.value += input.zoom * 4 * Time.deltaTime;

        // TODO: controller cursor lib??
        EventSystem.current.RaycastAll(pointer, raycasts);

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

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
        }

        if (input.cancel.WasPressed)
        {
            if (palettePanel.mode == PalettePanel.Mode.Colors)
            {
                palettePanel.SetMode(PalettePanel.Mode.Paint);
            }
        }

        /*
        if (Input.GetKeyDown(KeyCode.P))
        {
            TestCopy();
        }
        */
    }

    private void TestCopy()
    {
        List<object> memory = new List<object>();

        var timer = Stopwatch.StartNew();

        for (int i = 0; i < 512; ++i)
        {
            var copier = new Copier();
            memory.Add(copier.Copy(project));
        }

        timer.Stop();

        Debug.Log("Copies in " + timer.Elapsed.TotalSeconds);

        timer = Stopwatch.StartNew();

        for (int i = 0; i < 512; ++i)
        {
            memory.Add(JSON.Serialise(project));
        }

        timer.Stop();

        Debug.Log("Encodes in " + timer.Elapsed.TotalSeconds);
    }

    private void SetProject(Project project)
    {
        this.project = project;

        worldView.Setup(project.world);
        palettePanel.SetWorld(project.world);

        for (int i = 0; i < 16; ++i)
        {
            RefreshPalette(i);
        }
    }

    private Stamp stamp;

    public void SetStamp(Stamp stamp)
    {
        this.stamp = stamp;

        RefreshBrushCursor();
    }

    public void EditPalette(int i, Color color)
    {
        project.world.palette[i] = color;

        RefreshPalette(i);
    }

    public class PaletteChange : IChange
    {
        public Main main;
        public int index;
        public Color prev, next;

        void IChange.Redo(Changes changes)
        {
            main.EditPalette(index, next);
        }

        void IChange.Undo(Changes changes)
        {
            main.EditPalette(index, prev);
        }
    }

    public void RecordPaletteHistory(int i, Color prev, Color next)
    {
        //undos.Push(() => EditPalette(i, prev));

        var changes = new Changes();
        changes.changes[this] = new PaletteChange { main = this, index = i, prev = prev, next = next };

        Do(changes);
    }

    private void RefreshPalette(int i)
    {
        string name = string.Format("_Palette{0:D2}", i);

        material1.SetColor(name, project.world.palette[i]);
        material2.SetColor(name, project.world.palette[i]);
    }

    private GameObject hovering;
    private GameObject dragging;

    private bool clickedOnWorld;
    private bool clickingOnWorld;

    private Vector2 nextCursor, nextMouse;
    private Vector2 prevCursor, prevMouse;

    private Sprite brushSprite;
    [SerializeField] private SpriteRenderer brushRenderer;

    private void Update()
    {
        ///System.GC.Collect();

        if (locked) return;

        var color = Color.white * brightSlider.value;
        color.a = 1;

        brightImage.color = color;

        nextCursor = new Vector2((cursor.localPosition.x / 256f + 0.5f) * Screen.width,
                                 (cursor.localPosition.y / 256f + 0.5f) * Screen.height);

        worldView.actors.SetActive(project.world.actors);

        CheckHotkeys();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (Actor actor in project.world.actors)
            {
                actor.state.fragment = "bump";
                actor.state.line = 0;
            }
        }

        float interval = 0.1f;

        project.world.timer += Time.deltaTime;

        while (project.world.timer > interval)
        {
            project.world.timer -= interval;

            foreach (Actor actor in project.world.actors)
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

        foreach (Actor actor in project.world.actors)
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

        var plane = new Plane(Vector3.forward, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float t;
        plane.Raycast(ray, out t);
        Vector2 point = ray.GetPoint(t);

        Vector2 screenMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mouseCursorTransform.parent as RectTransform, Input.mousePosition, null, out screenMouse);
        mouseCursorTransform.localPosition = screenMouse;

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

        bool mouse = Input.GetMouseButton(0) && !mouseOverUI;
        bool gamep = input.click.IsPressed && !hovering;

        Vector2 prev = gamep ? prevCursor : prevMouse;
        Vector2 next = gamep ? nextCursor : nextMouse;

        prev.x = (int)prev.x;
        prev.y = (int)prev.y;
        next.x = (int)next.x;
        next.y = (int)next.y;

        byte value = (byte) palettePanel.selected;
        Blend<byte> blend_ = (canvas, brush) => brush == 0 ? canvas : value;

        brushRenderer.gameObject.SetActive(!mouseOverUI);
        brushRenderer.sprite = brushSpriteD.uSprite;
        brushRenderer.transform.position = next;

        if (!mouseOverUI
         && palettePanel.mode == PalettePanel.Mode.Colors)
        {
            SetCursorSprite(pickCursor);

            if ((Input.GetMouseButtonDown(0) || input.click.WasPressed))
            {
                int index = project.world.background.GetPixel(next);

                palettePanel.SelectPaletteIndex(index);
            }
        }
        else
        {
            if (mouseOverUI)
            {
                SetCursorSprite(normalCursor);
            }
            else if (stampToggle.isOn)
            {
                SetCursorSprite(stampCursor);
            }
            else
            { 
                SetCursorSprite(normalCursor);
            }
        }

        if ((mouse || gamep) && palettePanel.mode == PalettePanel.Mode.Paint)
        {
            if (!dragging_)
            {
                dragging_ = true;
                changes = new Changes();
            }
            else
            {
                /*
                if (freeToggle.isOn)
                {
                    var line = TextureByte.Pooler.Instance.Sweep(stamp.brush, prev, next, (canvas, brush) => brush == 0 ? canvas : brush);

                    {
                        project.world.background.Blend(changes, line, IntVector2.zero, blend_);

                        foreach (var actor in project.world.actors)
                        {
                            actor.Blend(changes, line, IntVector2.zero, blend_);
                        }
                    }

                    TextureByte.Pooler.Instance.FreeTexture(line.mTexture);
                    TextureByte.Pooler.Instance.FreeSprite(line);

                    changes.ApplyTextures();
                }
                */
                //else if (stampToggle.isOn)
                {
                    stampTimer -= (next - prev).magnitude;

                    if (stampTimer <= 0f)
                    {
                        stampTimer += 16;

                        project.world.background.Blend(changes, stamp.brush, next, blend_);

                        foreach (var actor in project.world.actors)
                        {
                            actor.Blend(changes, stamp.brush, next, blend_);
                        }

                        changes.ApplyTextures();
                    }
                }
            }
        }
        else
        {
            dragging_ = false;
            stampTimer = 0;

            if (changes != null)
            {
                Do(changes);
                changes = null;
            }
        }

        prevMouse = nextMouse;
        prevCursor = nextCursor;
    }

    private void RefreshBrushCursor()
    {
        byte value = (byte) palettePanel.selected;
        Blend<byte> blend_ = (canvas, brush) => brush == 0 ? (byte) 0 : value;

        brushSpriteD.Blend(stamp.brush, blend_);
        brushSpriteD.mTexture.Apply();
    }

    private Changes changes;
    private bool dragging_;
    private float stampTimer;

    public bool locked { get; private set; }

    public void Lock()
    {
        locked = true;
        saveBlocker.SetActive(true);
    }

    public void Unlock()
    {
        locked = false;
        saveBlocker.SetActive(false);
    }

    public IEnumerator LoadProject()
    {
        Lock();

        string path = Application.persistentDataPath + "/test.json.txt";
   
        var timer = Stopwatch.StartNew();

        var p = JSON.Deserialise<Project>(File.ReadAllText(path));

        yield return StartCoroutine(p.LoadFinalise());

        timer.Stop();
        Debug.Log("Loaded in " + timer.Elapsed.TotalSeconds);

        timer = Stopwatch.StartNew();

        yield return null;

        SetProject(p);

        timer.Stop();
        Debug.Log("Refreshed in " + timer.Elapsed.TotalSeconds);

        Unlock();
    }

    public IEnumerator SaveProject()
    {
        Lock();

        string path = Application.persistentDataPath + "/test.json.txt";

        var timer = Stopwatch.StartNew();
            
        yield return StartCoroutine(project.SaveFinalise());

        File.WriteAllText(path, JSON.Serialise(project));

        timer.Stop();
        Debug.Log("Saved in " + timer.Elapsed.TotalSeconds);

        Unlock();
    }

    public void SetCursorSprite(Sprite sprite)
    {
        Vector2 offset = sprite.pivot;

        var rtrans = mouseCursorImage.transform as RectTransform;

        mouseCursorImage.sprite = sprite;
        mouseCursorImage.SetNativeSize();
        rtrans.anchoredPosition = -offset;
    }
}
