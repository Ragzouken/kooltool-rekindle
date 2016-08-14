using UnityEngine;

public struct IntVector2 : System.IEquatable<IntVector2>
{
    public int x;
    public int y;

    public static IntVector2 Zero = new IntVector2(0, 0);
    public static IntVector2 One = new IntVector2(1, 1);

    public static IntVector2 Left  = new IntVector2(-1,  0);
    public static IntVector2 Right = new IntVector2( 1,  0);
    public static IntVector2 Up    = new IntVector2( 0,  1);
    public static IntVector2 Down  = new IntVector2( 0, -1);

    public IntVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVector2(float x, float y)
        : this((int) x, (int) y)
    {
    }

    public IntVector2 Offset(Vector2 offset)
    {
        return this + (IntVector2) offset;
    }

    public static implicit operator Vector2(IntVector2 point)
    {
        return new Vector2(point.x, point.y);
    }

    public static implicit operator Vector3(IntVector2 point)
    {
        return new Vector3(point.x, point.y, 0);
    }

    public static implicit operator IntVector2(Vector2 vector)
    {
        return new IntVector2(vector.x, vector.y);
    }

    public static implicit operator IntVector2(Vector3 vector)
    {
        return new IntVector2(vector.x, vector.y);
    }

    public override bool Equals (object obj)
    {
        if (obj is IntVector2)
        {
            return Equals((IntVector2) obj);
        }

        return false;
    }

    public bool Equals(IntVector2 other)
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

    public static bool operator ==(IntVector2 a, IntVector2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(IntVector2 a, IntVector2 b)
    {
        return a.x != b.x || a.y != b.y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
    {
        a.x += b.x;
        a.y += b.y;

        return a;
    }

    public static IntVector2 operator -(IntVector2 a, IntVector2 b)
    {
        a.x -= b.x;
        a.y -= b.x;

        return a;
    }
    
    public static IntVector2 operator *(IntVector2 a, int scale)
    {
        a.x *= scale;
        a.y *= scale;

        return a;
    }
}
