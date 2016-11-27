using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

namespace kooltool
{
    public partial class Scene : ICopyable<Scene>
    {
        public ImageGrid background = new ImageGrid();
        public TileMap tilemap = new TileMap();
        public HashSet<Actor> actors = new HashSet<Actor>();

        public void Copy(Copier copier, Scene copy)
        {
            copy.background = background;
            copy.actors = new HashSet<Actor>(actors.Select(actor => copier.Copy(actor)));
        }
    }

    public partial class Scene
    {
        public bool TryGetActor(IntVector2 position, 
                                out Actor actor,
                                int range=0)
        {
            foreach (var actor_ in actors)
            {
                actor = actor_;

                var rect = actor.GetWorldRect();
                rect.Expand(range);

                if (rect.Contains(position))
                {
                    for (int y = position.y - range; y < position.y + range + 1; ++y)
                    {
                        for (int x = position.x - range; x < position.x + range + 1; ++x)
                        {
                            if (actor.GetPixel(new IntVector2(x, y)) > 0) return true;
                        }
                    }
                }
            }

            actor = default(Actor);
            return false;
        }

        public byte GetPixel(IntVector2 position)
        {
            Actor actor;

            if (TryGetActor(position, out actor))
            {
                return actor.GetPixel(position);
            }
            else
            {
                return background.GetPixel(position);
            }
        }
    }
}
