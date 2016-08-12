using UnityEngine;

using Newtonsoft.Json;

[JsonObject(IsReference=false, MemberSerialization=MemberSerialization.OptIn)]
public struct Point : System.IEquatable<Point>
{
    [JsonProperty]
    public readonly int x;
    [JsonProperty]
    public readonly int y;

    public static Point Zero = new Point(0, 0);
    public static Point One = new Point(1, 1);

    public static Point Left  = new Point(-1,  0);
    public static Point Right = new Point( 1,  0);
    public static Point Up    = new Point( 0,  1);
    public static Point Down  = new Point( 0, -1);

    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Point(float x, float y)
        : this((int) x, (int) y)
    {
    }

    public Point(Vector2 point)
        : this(point.x, point.y)
    {
    }

    public Point Offset(Vector2 offset)
    {
        return this + new Point(offset);
    }

    public static implicit operator Vector2(Point point)
    {
        return new Vector2(point.x, point.y);
    }

    public static implicit operator Vector3(Point point)
    {
        return new Vector3(point.x, point.y, 0);
    }

    public static implicit operator Point(Vector2 vector)
    {
        return new Point(vector);
    }

    public static implicit operator Point(Vector3 vector)
    {
        return new Point((Vector2) vector);
    }

    public Point Size
    {
        get
        {
            return new Point(Mathf.Abs(x), Mathf.Abs(y));
        }
    }

    public override bool Equals (object obj)
    {
        if (obj is Point)
        {
            return Equals((Point) obj);
        }

        return false;
    }

    public bool Equals(Point other)
    {
        return other.x == x
            && other.y == y;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + x.GetHashCode();
        hash = hash * 23 + y.GetHashCode();
        return hash;
    }

    public override string ToString ()
    {
        return string.Format("Point({0}, {1})", x, y);
    }

    public static bool operator ==(Point a, Point b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Point a, Point b)
    {
        return a.x != b.x || a.y != b.y;
    }

    public static Point operator +(Point a, Point b)
    {
        return new Point(a.x + b.x, a.y + b.y);
    }

    public static Point operator -(Point a, Point b)
    {
        return new Point(a.x - b.x, a.y - b.y);
    }

    public static Point operator +(Point a, Vector2 b)
    {
        return new Point(a.x + b.x, a.y + b.y);
    }
    
    public static Point operator -(Point a, Vector2 b)
    {
        return new Point(a.x - b.x, a.y - b.y);
    }

    public static Point operator *(Point a, int scale)
    {
        return new Point(a.x * scale, a.y * scale);
    }
}
