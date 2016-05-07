using UnityEngine;
using System.Collections.Generic;

namespace PixelDraw
{
    public class TiledDrawing : IDrawing
    {
        public SparseGrid<IDrawing> Cells;
        protected SparseGrid<bool> Changed;

        SparseGrid<IDrawing>.Constructor NewCell;

        protected bool NullCell(Point coords, out IDrawing drawing)
        {
            drawing = null;

            return false;
        }

        public TiledDrawing(Point cellsize, 
                            SparseGrid<IDrawing>.Constructor newCell = null)
        {
            Cells = new SparseGrid<IDrawing>(cellsize.x, cellsize.y);
            Changed = new SparseGrid<bool>(cellsize.x, cellsize.y);

            NewCell = newCell ?? NullCell;
        }

        public virtual void Brush(Point pixel, Sprite image, Blend.BlendFunction blend)
        {
            pixel = pixel - new Point(image.pivot);
            
            Point grid, offset;
            
            Cells.Coords(pixel, out grid, out offset);
            
            var gw = Mathf.CeilToInt((image.rect.width  + offset.x) / Cells.CellWidth);
            var gh = Mathf.CeilToInt((image.rect.height + offset.y) / Cells.CellHeight);

            for (int y = 0; y < gh; ++y)
            {
                for (int x = 0; x < gw; ++x)
                {
                    IDrawing drawing;

                    Point cell = new Point(grid.x + x, grid.y + y);
                    
                    if (Cells.GetDefault(cell, out drawing, NewCell))
                    {
                        var point = new Point(x * Cells.CellWidth, y * Cells.CellHeight);

                        drawing.Brush(offset - point + image.pivot, image, blend);

                        Changed.Set(cell, true);
                    }
                }
            }
        }

        public virtual void Fill(Point pixel, Color color)
        {
            IDrawing drawing;
            Point grid, offset;
            
            Cells.Coords(pixel, out grid, out offset);
            
            if (Cells.Get(grid, out drawing))
            {
                drawing.Fill(offset, color);

                Changed.Set(grid, true);
            }
        }

        public virtual bool Sample(Point pixel, out Color color)
        {
            IDrawing drawing;
            Point grid, offset;
            
            Cells.Coords(pixel, out grid, out offset);
            
            if (Cells.Get(grid, out drawing))
            {
                return drawing.Sample(offset, out color);
            }
            else
            {
                color = Color.black;
                
                return false;
            }
        }
        
        public virtual void Apply()
        {
            foreach (var changed in Changed)
            {
                IDrawing drawing;
                
                if (changed.Value && Cells.Get(changed.Key, out drawing))
                {
                    drawing.Apply();
                }
            }
            
            Changed.Clear();
        }

        public virtual Sprite Sample(Rect area)
        {
            var texture = BlankTexture.New((int) area.width, 
                                           (int) area.height, 
                                           new Color(0, 0, 0, 0));

            var sprite = texture.FullSprite();

            Point grid, offset;

            Cells.Coords(area.min, out grid, out offset);

            var gw = Mathf.CeilToInt((area.width + offset.x) / Cells.CellWidth);
            var gh = Mathf.CeilToInt((area.height + offset.y) / Cells.CellHeight);

            for (int y = 0; y < gh; ++y)
            {
                for (int x = 0; x < gw; ++x)
                {
                    IDrawing drawing;

                    Point cell = new Point(grid.x + x, grid.y + y);

                    if (Cells.Get(cell, out drawing))
                    {
                        var point = new Point(x * Cells.CellWidth, y * Cells.CellHeight);
                        var brush = ((SpriteDrawing) drawing).Sprite;

                        PixelDraw.Brush.Apply(brush, 
                                              cell * Cells.CellWidth, 
                                              sprite, 
                                              area.min, 
                                              Blend.Replace);
                    }
                }
            }

            return sprite;
        }
    }
}
