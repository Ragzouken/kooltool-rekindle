﻿using UnityEngine;
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

using InputField = TMPro.TMP_InputField;
using Stopwatch = System.Diagnostics.Stopwatch;

using kooltool;

public class Main : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Delete Prefs")]
    public static void DeletePrefs()
    {
        PlayerPrefs.DeleteAll();
    }
#endif

    [SerializeField] private HUD hud;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WorldView worldView;

    [SerializeField] private SpriteRenderer pointerRenderer;
    [SerializeField] private Sprite pointerSprite;
    [SerializeField] private Trail trail;

    [SerializeField] private DialogueBox dialogueBox;
    [SerializeField] private Toggle followToggle;

    [SerializeField] private Slider zoomSlider;
    [SerializeField] private Texture2D costumeTexture;

    [SerializeField] private RectTransform cursor;

    [SerializeField] private Image brightImage;
    [SerializeField] private Slider brightSlider;
    [SerializeField] private DrawHUD drawHUD;

    [SerializeField] private Material material1;
    [SerializeField] private Material material2;
    [SerializeField] private GameObject saveBlocker;

    [SerializeField] private RectTransform mouseCursorTransform;
    [SerializeField] private Image mouseCursorImage;
    [SerializeField] private Sprite normalCursor;
    [SerializeField] private Sprite pickCursor, stampCursor;

    [SerializeField] private SpriteRenderer borderTest;
    [SerializeField] private SpriteRenderer cellCursor;
    [SerializeField] private SpriteRenderer regCursor;
    [SerializeField] private Sprite cellBorder;

    public Project project { get; private set; }
    private Scene saved;

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

    private InstancePool<Stamp> stampsp;

    public Sprite[] testbrushes;
    private ManagedSprite<byte> brushSpriteD;

    [Header("Character Toggles")]
    [SerializeField]
    private Toggle addCharacter;
    [SerializeField]
    private Toggle removeCharacter;
    [SerializeField]
    private InputField characterDialogue;
    [SerializeField]
    private Sprite characterSelectCursor;
    [SerializeField]
    private AudioSource characterPlaceSound;
    [SerializeField]
    private AudioSource undoSound;

    [SerializeField]
    private Sprite enterActorCursor, exitActorCursor;

    public bool playing
    {
        get
        {
            return playScene != null;
        }
    }

    private Actor dialogueActor;

    public bool dialogue
    {
        get
        {
            return dialogueActor != null;
        }
    }
    
    private void FillSimpleProject()
    {
        var costume = NewSimpleCostume();
        var scene = project.scenes.First();
        
        var actor = new Actor
        {
            costume = costume,
            world = scene,
            dialogue = "player",
            position = new Position(IntVector2.one * 16),
        };

        scene.actors.Add(actor);

        possessedActor = actor;
    }

    private Costume NewCostume()
    {
        var costume = project.CreateCostume4d1();
        costume.down.mTexture.SetPixels(test.pixels);
        costume.down.mTexture.Apply();

        return costume;
    }

    private Costume NewSimpleCostume()
    {
        var rect = new IntRect(0, 0, 32, 32);

        var texture = new KoolTexture(32, 32);
        texture.SetPixels(test.GetPixels(new IntRect(0, 64, 32, 32)));
        texture.Apply();

        var spr = new KoolSprite(texture, new IntRect(0, 0, 32, 32), IntVector2.one * 16);

        var costume = new Costume
        {
            right = spr,
            down  = spr,
            left  = spr,
            up    = spr,
        };

        return costume;
    }

    private Costume defaultCostume;

    private ManagedSprite<Color32> pointerMSprite;

    private void Start()
    { 
        Cursor.visible = false;

        {
            var texture = new TextureColor32(19, 19);
            pointerMSprite = new ManagedSprite<Color32>(texture, new IntRect(0, 0, 19, 19), new IntVector2(10, 10));

            pointerMSprite.SetPixels(pointerSprite.texture.GetPixels32());
        }

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

        brushSpriteD = new TextureByte(64, 64).FullSprite(IntVector2.one * 32);

        /*
        string path = Application.streamingAssetsPath + @"\test.txt";
        var script = ScriptFromCSV(File.ReadAllText(path));
        */

        stampsp = new InstancePool<Stamp>(stampPrefab, stampParent);

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

        drawHUD.OnPaletteIndexSelected += i => RefreshBrushCursor();

        project = new Project();
        var w = new Scene();

        var palette = new Palette();
        project.AddPalette(palette);

        for (int i = 1; i < 16; ++i)
        {
            palette.colors[i] = new Color(Random.value, Random.value, Random.value, 1f);
        }

        project.AddScene(w);
        w.background.project = project;
        w.background.cellSize = 256;
        SetProject(project);

        for (int i = 0; i < 16; ++i)
        {
            var next = Vector2.right * Random.Range(-4, 4) * 32
                     + Vector2.up    * Random.Range(-4, 4) * 32;

            /*
            project.world.actors.Add(new Actor
            {
                world = project.world,
                costume = costume,
                //script = script,
                state = new State { fragment = "start", line = 0 },
                position = new Position
                {
                    prev = next,
                    next = next + Vector2.right * 32,
                    progress = 0,
                },
            });
            */
        }

        saved = Copier.EZCopy(project.scenes.First());

        //Debug.LogFormat("Original:\n{0}", File.ReadAllText(path));

        /*
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
        */

        //Application.OpenURL(path);

        hud.mode = HUD.Mode.Draw;

        input = new TestInputSet();

        borderSprite0 = TextureByte.Pooler.Instance.GetSprite(40, 40, IntVector2.one * 20);
        borderSprite1 = TextureByte.Pooler.Instance.GetSprite(40, 40, IntVector2.one * 20);

        defaultCostume = NewCostume();

        FillSimpleProject();

        LoadGistAgain();

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
        public PlayerOneAxisAction turn;
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
                var cw = CreatePlayerAction("Turn CW");
                var acw = CreatePlayerAction("Turn ACW");

                cw.AddDefaultBinding(Key.E);
                acw.AddDefaultBinding(Key.Q);

                turn = CreateOneAxisPlayerAction(cw, acw);
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

                zoomIn.AddDefaultBinding(Key.Equals);
                zoomOut.AddDefaultBinding(Key.Minus);

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

    public void Undo()
    {
        if (undos.Count == 0) return;

        var change = undos.Pop();

        change.Undo();

        redos.Push(change);

        undoSound.Play();
    }

    public void Redo()
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
    private static extern string GetWindowSearch();

    private void FromGist(string id)
    {
        StartCoroutine(Gist.Download(id, (System.Action<Dictionary<string, string>>)((Dictionary<string, string> dict) =>
        {
            var scene = Enumerable.First<Scene>(this.project.scenes);

            //scene.palette = BytesToColors(FromBase64(dict["palette"]));

            dict.Remove("palette");

            foreach (var pair in dict)
            {
                string[] coords = pair.Key.Split(',');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);

                byte[] data = System.Convert.FromBase64String(pair.Value);

                var c = scene.background.AddCell(new IntVector2(x, y));
                // TODO: loading
                //c.mTexture.DecodeFromPNG(data);
            }

            SetProject((Project) this.project);
        })));
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
        var scene = project.scenes.First();
        var background = scene.background.cells.ToDictionary(p => string.Format("{0},{1}", p.Key.x, p.Key.y),
                                                             p => ToBase64(p.Value.mTexture.uTexture.EncodeToPNG()));

        background["palette"] = ToBase64(ColorsToBytes(project.palettes.Single().colors));

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

        /*
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            StartCoroutine(LoadProject());
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            StartCoroutine(SaveProject());
        }
        */

        if (Input.GetKeyDown(KeyCode.Slash))
        {
            string text = JSON.Serialise(new[] { test, test, test });
            var textures = JSON.Deserialise<TextureByte[]>(text);

            Debug.Log(text);
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

        if (playing)
            return;

        /*
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Redo();
        }
        */
    }

    private void LoadGistAgain()
    {
        StartCoroutine(Gist.Download("0a3f88ac668f435dff26250390cda2e2", gist =>
        {
            SetProject(Project.FromGist(gist));
        }));
    }

    private void SaveGistAgain()
    {
        var gist = project.ToGist();

        StartCoroutine(Gist.Create("test gist",
                       project.ToGist(),
                       id => Debug.Log(id)));
    }

    private Scene editScene;
    private Scene playScene;

    public void EnterPlayMode()
    {
        hud.mode = HUD.Mode.Play;

        playScene = Copier.EZCopy(editScene);

        timer = 0;
        SetScene(playScene);

        possessedActor = playScene.actors.FirstOrDefault(actor => actor.dialogue.StartsWith("player"));

        SaveGistAgain();
    }

    public void ExitPlayMode()
    {
        hud.mode = HUD.Mode.Draw;

        playScene = null;
        possessedActor = null;

        SetScene(editScene);
    }

    private void SetProject(Project project)
    {
        this.project = project;

        editScene = project.scenes.Single();
        SetScene(editScene);

        for (int i = 0; i < 16; ++i)
        {
            RefreshPalette(i);
        }
    }

    private void SetScene(Scene scene)
    {
        worldView.SetConfig(scene);
        drawHUD.SetWorld(scene);
    }

    private Stamp stamp;

    public void SetStamp(Stamp stamp)
    {
        this.stamp = stamp;

        RefreshBrushCursor();
    }

    public void EditPalette(int i, Color color)
    {
        project.palettes.Single().colors[i] = color;

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
        
        var palette = project.palettes.Single();

        material1.SetColor(name, palette.colors[i]);
        material2.SetColor(name, palette.colors[i]);
    }

    private GameObject hovering;
    private GameObject dragging;
    private Actor targetActor;

    private bool clickedOnWorld;
    private bool clickingOnWorld;

    private Vector2 nextCursor, nextMouse;
    private Vector2 prevCursor, prevMouse;

    private Sprite brushSprite;
    [SerializeField] private SpriteRenderer brushRenderer;

    private int stippleOffset;
    private int stippleStride = 8;

    [SerializeField] private Slider stippleSlider;

    private ManagedSprite<byte> borderSprite0;
    private ManagedSprite<byte> borderSprite1;

    public int borderSize = 1;

    private void UpdateBorder(Actor actor)
    {

        borderTest.transform.localPosition = actor.position.current;

        borderSprite0.Clear(0);
        borderSprite0.Blend(actor.costume[actor.position.direction], TextureByte.mask);

        int w = borderSprite0.mTexture.width;
        int h = borderSprite0.mTexture.height;

        for (int i = 0; i < borderSize; ++i)
        { 
            borderSprite1.Clear(0);

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if ((x > 0   && borderSprite0.mTexture.pixels[(x - 1) * w + y] > 0)
                     || (y > 0   && borderSprite0.mTexture.pixels[(x) * w + y - 1] > 0)
                     || (x < w-1 && borderSprite0.mTexture.pixels[(x + 1) * w + y] > 0)
                     || (y < h-1 && borderSprite0.mTexture.pixels[(x) * w + y + 1] > 0))
                    {
                        borderSprite1.mTexture.pixels[x * w + y] = 3;
                    }
                }
            }

            borderSprite0.mTexture.Clear(0);
            borderSprite0.mTexture.SetPixels(borderSprite1.mTexture.pixels);
        }

        borderTest.sprite = borderSprite1.uSprite;
        borderSprite1.mTexture.Apply();
    }

    Vector2 prev, next;
    bool mouseHold, mousePress;
    float timer;

    private void Update()
    {
        ///System.GC.Collect();

        if (locked) return;

        if (dialogue)
        {
            dialogueBox.Show(dialogueActor.dialogue);

            if (Input.anyKeyDown)
            {
                dialogueActor = null;
                dialogueBox.Hide();
            }
        }

        var color = Color.white * brightSlider.value;
        color.a = 1;

        brightImage.color = color;

        nextCursor = new Vector2((cursor.localPosition.x / 256f + 0.5f) * Screen.width,
                                 (cursor.localPosition.y / 256f + 0.5f) * Screen.height);

        CheckHotkeys();

        float interval = 0.1f;

        var scene = playScene ?? editScene;

        timer += Time.deltaTime;
        worldView.Refresh();

        while (timer > interval)
        {
            timer -= interval;

            foreach (Actor actor in scene.actors)
            {
                if (actor.position.moving) continue;

                while (true && false)
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

        foreach (Actor actor in scene.actors)
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

        //UpdateBorder(project.world.actors.First());

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

        mouseHold  = Input.GetMouseButton(0);
        mousePress = Input.GetMouseButtonDown(0) && !mouseOverUI;

        bool mouse = Input.GetMouseButton(0) && !mouseOverUI;
        bool gamep = input.click.IsPressed && !hovering;

        prev = gamep ? prevCursor : prevMouse;
        next = gamep ? nextCursor : nextMouse;

        #region angle bullshit
        var delta2 = next - prev;
        if (delta2.magnitude > 1)
        {
            float a = Mathf.Atan2(delta2.y, delta2.x);
            angles.Enqueue(a);
        }
        else if (angles.Count > 0)
        {
            angles.Enqueue(angles.Peek());
        }

        if (angles.Count > 3) angles.Dequeue();
        if (angles.Count > 0) angle = angles.Average();
        #endregion

        prev.x = Mathf.FloorToInt(prev.x);
        prev.y = Mathf.FloorToInt(prev.y);
        next.x = Mathf.FloorToInt(next.x);
        next.y = Mathf.FloorToInt(next.y);

        SetCursorSprite(normalCursor);
        
        if (hud.mode != HUD.Mode.Draw)
        {
            drawHUD.expanded = false;
        }

        UpdateCharacterMovement();

        if (possessedActor != null)
        {
            var view = worldView.actors.Get(possessedActor);

            //pointerRenderer.gameObject.SetActive(true);
            trail.gameObject.SetActive(true);
            

            float angle1 = (Time.timeSinceLevelLoad * 3) % (2 * Mathf.PI);
            float ox = Mathf.Cos(angle1) * 24;
            float oy = Mathf.Sin(angle1) * 24;

            trail.next = (Vector2) view.transform.position
                       + new Vector2(ox, oy);

            /*
            float angle2 = angle1 * Mathf.Rad2Deg + 180;
            int rots = (int) (angle2 + 45) / 90;

            var spr = TextureColor32.Pooler.Instance.Rotated(pointerMSprite, rots);
            spr.mTexture.Apply();

            pointerRenderer.sprite = spr.uSprite;

            pointerRenderer.transform.position = view.transform.position
                                               + new Vector3(ox, oy);
            //pointerRenderer.transform.localEulerAngles = Vector3.forward * (angle1 * Mathf.Rad2Deg + 180);
            */
        }
        else
        {
            pointerRenderer.gameObject.SetActive(false);
            trail.gameObject.SetActive(false);
        }

        if (hud.mode == HUD.Mode.Play)
        {
            UpdatePlayInput();
        }
        else
        { 
            CleanupPlay();
        }

        if (hud.mode == HUD.Mode.Draw && drawHUD.mode == DrawHUD.Mode.Paint)
        {
            UpdatePaintInput();
        }
        else
        { 
            CleanupPaint();
        }

        if (hud.mode == HUD.Mode.Draw && drawHUD.mode == DrawHUD.Mode.Colors)
        {
            UpdateColorsInput();
        }

        if (hud.mode == HUD.Mode.Character)
        {
            UpdateCharacterInput();
        }
        else
        {
            CleanupCharacter();
        }

        prevMouse = nextMouse;
        prevCursor = nextCursor;
    }

    private void EndStroke()
    {
        dragging_ = false;
        targetActor = null;
        stippleOffset = 0;

        if (changes != null)
        {
            Do(changes);
            changes = null;
        }
    }

    private void UpdateColorsInput()
    {
        if (input.cancel.WasPressed)
        {
            drawHUD.SetMode(DrawHUD.Mode.Paint);
            return;
        }

        if (!mouseOverUI)
        {
            SetCursorSprite(pickCursor);

            if ((Input.GetMouseButtonDown(0) || input.click.WasPressed))
            {
                int index = project.scenes.First().GetPixel(next);

                drawHUD.SelectPaletteIndex(index);
            }
        }
    }

    private void UpdatePlayInput()
    {
        if (input.cancel.WasPressed)
        {
            ExitPlayMode();
        }
    }

    private void CleanupPlay()
    {

    }

    private void UpdatePaintInput()
    {
        if (input.cancel.WasPressed)
        {
            if (drawHUD.expanded)
            {
                drawHUD.expanded = false;
                return;
            }

            //hud.mode = HUD.Mode.Switch;
            return;
        }

        brushRenderer.sprite = brushSpriteD.uSprite;
        brushRenderer.transform.position = next;

        Actor actor;

        var scene = project.scenes.First();

        if (scene.TryGetActor(next, out actor, 3))
        {
            brushRenderer.sortingLayerName = "World - Actors";
            brushRenderer.sortingOrder = 1;
        }
        else
        {
            brushRenderer.sortingLayerName = "World - Background";
            brushRenderer.sortingOrder = 1;
        }

        brushRenderer.gameObject.SetActive(dragging_ || !mouseOverUI);

        bool mouseCounts = (dragging_ && mouseHold) || mousePress;

        if (mouseCounts)
        {
            if (!dragging_)
            {
                dragging_ = true;
                changes = new Changes();
                scene.TryGetActor(next, out targetActor, 3);
            }
            else
            {
                var line = TextureByte.Pooler.Instance.Sweep(brushSpriteD, 
                                                prev, 
                                                next, 
                                                (canvas, brush) => brush == 0 ? canvas : brush,
                                                (int) stippleSlider.value,
                                                ref stippleOffset);

                byte value = (byte) drawHUD.selected;
                Blend<byte> blend_ = (canvas, brush) => brush == 0 ? canvas : value;

                if (targetActor != null)
                {
                    targetActor.Blend(changes, line, IntVector2.zero, blend_);
                }
                else
                {
                    scene.background.Blend(changes, line, IntVector2.zero, blend_);
                }

                TextureByte.Pooler.Instance.FreeTexture(line.mTexture);
                TextureByte.Pooler.Instance.FreeSprite(line);

                changes.ApplyTextures();
            }
        }
        else
        {
            EndStroke();
        }

        RefreshBrushCursor();
    }

    private void CleanupPaint()
    {
        brushRenderer.gameObject.SetActive(false);
        EndStroke();
    }

    private Actor possessedActor;

    private MaterialPropertyBlock cursorBlock;

    private void UpdateCharacterInput()
    {
        IntVector2 cell = ((IntVector2) next).CellCoords(32);

        regCursor.gameObject.SetActive(true);
        regCursor.transform.localPosition = cell * 32 + IntVector2.one * 16;

        if (addCharacter.isOn)
        {
            cellCursor.gameObject.SetActive(true);
            
            cellCursor.transform.localPosition = cell * 32 + IntVector2.one * 16;

            cursorBlock = cursorBlock ?? new MaterialPropertyBlock();
            cellCursor.GetPropertyBlock(cursorBlock);

            cursorBlock.SetFloat("_Cycle", Time.timeSinceLevelLoad * 12);
            cellCursor.SetPropertyBlock(cursorBlock);
            cellCursor.sprite = defaultCostume.down.uSprite;
        }
        else
        {
            cellCursor.gameObject.SetActive(false);
        }

        if (input.cancel.WasPressed)
        {
            hud.mode = HUD.Mode.Draw;
            CleanupCharacter();
            return;
        }

        characterDialogue.interactable = possessedActor != null;

        if (possessedActor != null)
        {
            if (characterDialogue.isFocused)
            {
                possessedActor.dialogue = characterDialogue.text;
            }
            else
            {
                characterDialogue.text = possessedActor.dialogue;
            }
        }
        else
        {
            characterDialogue.text = "NO CHARACTER SELECTED";
        }

        Actor hoveredActor;

        var scene = project.scenes.First();    

        scene.TryGetActor(next, out hoveredActor, 0);

        if (hoveredActor != null)
        {
            if (hoveredActor == possessedActor)
            {
                SetCursorSprite(exitActorCursor);
            }
            else
            {
                SetCursorSprite(enterActorCursor);
            }
        }

        if (mousePress && hoveredActor != null)
        {
            if (removeCharacter.isOn)
            {
                scene.actors.Remove(hoveredActor);

                if (possessedActor == hoveredActor)
                    possessedActor = null;

                var changes = new Changes();
                changes.GetChange(hoveredActor, () => new ActorRemovedChange { world = scene, actor = hoveredActor });
                Do(changes);

                removeCharacter.isOn = false;
            }
            else
            {
                if (possessedActor == hoveredActor)
                {
                    possessedActor = null;
                    cameraController.focusTarget = hoveredActor.position.current - Vector2.one * 16;
                }
                else
                {
                    possessedActor = hoveredActor;
                    characterDialogue.text = possessedActor.dialogue;
                }
            }
        }
        else if (mousePress && addCharacter.isOn)
        {
            var pos = ((IntVector2) next).CellCoords(32) * 32 + IntVector2.one * 16;

            var actor = new Actor
            {
                world = scene,
                costume = NewSimpleCostume(),
                state = new State { fragment = "start", line = 0 },
                position = new Position
                {
                    prev = pos,
                    next = pos,
                    progress = 0,
                },
            };

            scene.actors.Add(actor);

            var changes = new Changes();
            changes.GetChange(actor, () => new ActorAddedChange { world = scene, actor = actor });
            Do(changes);

            characterPlaceSound.Play();
            addCharacter.isOn = false;
            possessedActor = actor;
        }
    }

    private void UpdateCharacterMovement()
    {
        if (possessedActor != null)
        {
            cameraController.focusTarget = possessedActor.position.current;

            if (dialogue)
                return;

            if (characterDialogue.isFocused)
                return;

            if (input.turn.WasPressed)
            {
                var pos = possessedActor.position;

                if (input.turn.Value > 0)
                {
                    pos.direction = (Position.Direction) (((int) pos.direction + 3) % 4);
                }
                else if (input.turn.Value < 0)
                {
                    pos.direction = (Position.Direction) (((int) pos.direction + 1) % 4);
                }
            }

            if (!possessedActor.position.moving)
            {
                var move = Vector2.zero;
                var direction = possessedActor.position.direction;

                if (input.move.Left.IsPressed)
                {
                    move = Vector2.left * 32;
                    direction = Position.Direction.Left;
                }
                else if (input.move.Right.IsPressed)
                {
                    move = Vector2.right * 32;
                    direction = Position.Direction.Right;
                }
                else if (input.move.Up.IsPressed)
                {
                    move = Vector2.up * 32;
                    direction = Position.Direction.Up;
                }
                else if (input.move.Down.IsPressed)
                {
                    move = Vector2.down * 32;
                    direction = Position.Direction.Down;
                }

                Vector2 next = possessedActor.position.prev + move;
                Actor collider = null;

                var scene = playScene;

                bool blocked = playing 
                            && scene.TryGetActor(next, out collider)
                            && collider != possessedActor;

                if (!blocked)
                {
                    possessedActor.position.next = next;
                }
                else
                {
                    dialogueActor = collider;
                }

                possessedActor.position.direction = direction;
            }
        }
    }

    private void CleanupCharacter()
    {
        addCharacter.isOn = false;
        removeCharacter.isOn = false;
        cellCursor.gameObject.SetActive(false);
        regCursor.gameObject.SetActive(false);
        characterDialogue.DeactivateInputField();
    }

    private ManagedSprite<byte> shearSprite;
    private float angle;
    private Queue<float> angles = new Queue<float>();

    private MaterialPropertyBlock drawCursorBlock;

    private void RefreshBrushCursor()
    {
        drawCursorBlock = drawCursorBlock ?? new MaterialPropertyBlock();

        float quarter = Mathf.PI * 0.5f;

        this.angle = (this.angle + Mathf.PI * 2) % (Mathf.PI * 2);

        var angle = this.angle % quarter;
        int rots = Mathf.FloorToInt(this.angle / quarter + 3) % 4;

        //*
        if (angle > quarter * 0.5f)
        {
            angle -= quarter;
            rots = (rots + 1) % 4;
        }
        //*/

        float alpha = -Mathf.Tan(angle / 2f);
        float beta = Mathf.Sin(angle);

        //var shearSprite4 = TextureByte.Pooler.Instance.ShearX(stamp.brush, Time.timeSinceLevelLoad % 1);

        byte value = (byte) drawHUD.selected;
        Blend<byte> blend_ = (canvas, brush) => brush == 0 ? (byte) 0 : value;

        brushRenderer.GetPropertyBlock(drawCursorBlock);

        if (value == 0)
        {
            drawCursorBlock.SetFloat("_Cycle", Time.timeSinceLevelLoad * 16);
            
            blend_ = (canvas, brush) => brush == 0 ? (byte) 0 : (byte) 1;
        }
        else
        {
            drawCursorBlock.SetFloat("_Cycle", 0);
        }

        brushRenderer.SetPropertyBlock(drawCursorBlock);

        brushSpriteD.Clear(0);

        if (followToggle.isOn)
        {
            var shearSprite1 = TextureByte.Pooler.Instance.Rotated(stamp.brush, rots);
            //var shearSprite1 = TextureByte.Pooler.Instance.Copy(stamp.brush);
            var shearSprite2 = TextureByte.Pooler.Instance.ShearX(shearSprite1, alpha);
            TextureByte.Pooler.Instance.FreeTexture(shearSprite1.mTexture);
            TextureByte.Pooler.Instance.FreeSprite(shearSprite1);
            var shearSprite3 = TextureByte.Pooler.Instance.ShearY(shearSprite2, beta);
            TextureByte.Pooler.Instance.FreeTexture(shearSprite2.mTexture);
            TextureByte.Pooler.Instance.FreeSprite(shearSprite2);
            var shearSprite4 = TextureByte.Pooler.Instance.ShearX(shearSprite3, alpha);
            TextureByte.Pooler.Instance.FreeTexture(shearSprite3.mTexture);
            TextureByte.Pooler.Instance.FreeSprite(shearSprite3);

            ////var shearSprite4 = shearSprite1;

            brushSpriteD.Blend(shearSprite4, blend_);
            TextureByte.Pooler.Instance.FreeTexture(shearSprite4.mTexture);
            TextureByte.Pooler.Instance.FreeSprite(shearSprite4);
        }
        else
        {
            brushSpriteD.Blend(stamp.brush, blend_);
        }

        Actor actor = targetActor;

        if (project == null)
            return;

        var scene = project.scenes.First();

        if (actor != null || scene.TryGetActor(next, out actor, 3))
        {
            brushSpriteD.Crop(actor.costume[actor.position.direction],
                              canvasPosition: next, 
                              brushPosition: actor.position.current);
        }

        brushSpriteD.mTexture.Apply();
    }

    private Changes changes;
    private bool dragging_;

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

        // TODO: fix
        //yield return StartCoroutine(p.LoadFinalise());

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

        yield return null;
           // TODO: fix
        //yield return StartCoroutine(project.SaveFinalise());

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
