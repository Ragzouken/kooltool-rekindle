using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

using Newtonsoft.Json;

namespace kooltool
{
    [JsonObject(IsReference = false)]
    public partial class Project
    {
        public HashSet<KoolTexture> textures = new HashSet<KoolTexture>();
        public HashSet<Costume> costumes = new HashSet<Costume>();
        public HashSet<Scene> scenes = new HashSet<Scene>();
        public HashSet<Palette> palettes = new HashSet<Palette>();
    }

    public partial class Project
    {
        public void AddScene(Scene scene)
        {
            scenes.Add(scene);
        }

        public void AddTexture(KoolTexture texture)
        {
            textures.Add(texture);
        }

        public void AddCostume(Costume costume)
        {
            costumes.Add(costume);
        }

        public void AddPalette(Palette palette)
        {
            palettes.Add(palette);
        }
    }

    public partial class Project
    {
        public KoolTexture CreateTexture(int width, int height)
        {
            var texture = new KoolTexture(width, height);

            AddTexture(texture);

            return texture;
        }

        public Costume CreateCostume4d1()
        {
            var texture = CreateTexture(32, 128);
            var costume = new Costume
            {
                up    = new KoolSprite(texture, new IntRect(0,  0, 32, 32), new IntVector2(16, 16)),
                down  = new KoolSprite(texture, new IntRect(0, 32, 32, 32), new IntVector2(16, 16)),
                left  = new KoolSprite(texture, new IntRect(0, 64, 32, 32), new IntVector2(16, 16)),
                right = new KoolSprite(texture, new IntRect(0, 96, 32, 32), new IntVector2(16, 16)),
            };
            
            AddCostume(costume);

            return costume;
        }
    }

    public partial class Project
    {
        [JsonObject(IsReference = false)]
        public class ProjectSave
        {
            [JsonConverter(typeof(DictionarySerializer<object, string, Index>))]
            //[JsonArray]
            public class Index : Dictionary<object, string> { }

            public Project project;
            public Index resources = new Index();
        }

        public static Project FromGist(Dictionary<string, string> gist)
        {
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(gist["project"]));

            Debug.Log(data);

            var save = JSON.Deserialise<ProjectSave>(data);

            Debug.Log("Finished deserialize");

            foreach (var texture in save.project.textures)
            {
                string id = save.resources[texture];
                byte[] tex = Convert.FromBase64String(gist[id]);

                texture.DecodeFromPNG(tex);
            }

            Debug.Log("finished png loading");

            return save.project;
        }

        public Dictionary<string, string> ToGist()
        {
            var gist = new Dictionary<string, string>();
            var save = new ProjectSave { project = this, };

            foreach (var texture in textures)
            {
                string id = Guid.NewGuid().ToString();
                byte[] data = texture.uTexture.EncodeToPNG();

                save.resources.Add(texture, id);
                gist.Add(id, Convert.ToBase64String(data));
            }

            string serialized = JSON.Serialise(save, true);

            Debug.Log(serialized);

            gist.Add("project", Convert.ToBase64String(Encoding.UTF8.GetBytes(serialized)));

            return gist;
        }

        public void ToDisk()
        {
            var save = new ProjectSave { project = this, };

            foreach (var texture in textures)
            {
                string id = Guid.NewGuid().ToString();
                byte[] data = texture.uTexture.EncodeToPNG();
                string dest = string.Format("{0}/{1}.png",
                                            Application.persistentDataPath,
                                            id);

                save.resources.Add(texture, id);
                System.IO.File.WriteAllBytes(dest, data);
            }

            string serialized = JSON.Serialise(save);
            string dest2 = string.Format("{0}/project.json.txt", Application.persistentDataPath);

            System.IO.File.WriteAllText(dest2, serialized);
        }
    }
}
