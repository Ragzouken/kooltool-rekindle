using UnityEngine.Assertions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Serialization for simple tree structures
/// - aliased references will result in two copies
/// - circular references will result in infinite recursion
/// - both parties must register the same types in the same order
/// </summary>
public class Serialization // TODO: should probably not have everything static
{
    #region Type Management

    #region Signature

    public struct Signature
    {
        public byte[] hash;
    }

    public static Signature signature
    {
        get
        {
            var sig = string.Join("\n", types.Select(type => type.FullName).ToArray());
            var hasher = System.Security.Cryptography.SHA512.Create();

            return new Signature
            {
                hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(sig)),
            };
        }
    }

    #endregion

    private struct Serializable
    {
        public Action<BinaryWriter, object> serialize;
        public Func<BinaryReader, object> deserialize;
        public bool reference;
    }

    private class Null { };
    private class Reference { };
    private class Array { };

    /// <summary>
    /// Ordered list of known serializable types.
    /// Type information is serialized as the index within this list.
    /// null is represented as a dummy type at index 0.
    /// T[] is represented as a dummy type at index 1.
    /// </summary>
    private static IList<Type> types = new List<Type>();

    /// <summary>
    /// Map of type to serialize/deserialize functions
    /// </summary>
    private static Dictionary<Type, Serializable> overrides
        = new Dictionary<Type, Serializable>();
        
    static Serialization()
    {
        RegisterType<Null>((writer, value) => { },
                           (reader)        => null);

        types.Add(typeof(Array));

        RegisterType((writer, value) => writer.Write(value),
                     (reader)        => reader.ReadInt32());

        RegisterType((writer, value) => writer.Write(value),
                     (reader)        => reader.ReadString());

        RegisterType((writer, value) => writer.Write(value),
                     (reader)        => reader.ReadSingle());

        RegisterType((writer, value) => writer.Write(value),
                     (reader)        => reader.ReadBoolean());

        RegisterType((writer, value) => { writer.Write(value.Length); writer.Write(value); },
                     (reader)        => { int l = reader.ReadInt32(); return reader.ReadBytes(l); });

        UseDefault<Signature>();

        RegisterAbstract<object>();
    }

    public static void UseDefault<T>()
    {
        RegisterType((writer, value) => SerializeObject(writer, value, typeof(T)),
                        (reader)        => ((T) DeserializeObject(reader, typeof(T))));
    }

    public static void RegisterEnum<T>()
    {
        Assert.IsTrue(typeof(T).IsEnum, string.Format("Type {0} is not an Enum!", typeof(T).Name));

        // we can't constrain T to be an enum so we must cast to object
        // between the real casts so type checking doesn't complain
        RegisterType((writer, value) => writer.Write((int) (object) value),
                     (reader)        => (T) (object) reader.ReadInt32());
    }

    public static void RegisterAbstract<T>()
    {
        // we'll never be given objects of abstract types, so we'll never
        // try to serialize them unless something goes wrong - we use them
        // only as markers to help deserialise nested arrays
        RegisterType((writer, value) => { throw new Exception(string.Format("Can't serialize abstract type {0}!", typeof(T).Name)); },
                        (reader)        => { throw new Exception(string.Format("Can;t deserialize abstract type {0}!", typeof(T).Name)); return default(T); });
    }

    public static void RegisterType<T>(Action<BinaryWriter, T> serialize,
                                       Func<BinaryReader, T> deserialize,
                                       bool reference=false)
    {
        types.Add(typeof(T));

        overrides[typeof(T)] = new Serializable
        {
            serialize   = (writer, @object) => serialize(writer, (T) @object),
            deserialize = (reader)          => deserialize(reader),
            reference   = reference
        };
    }

    #endregion

    #region Serialization

    public static void WriteType(BinaryWriter writer, Type type)
    {
        Assert.IsTrue(types.Contains(type), string.Format("Type {0} is not recognised!", type.Name));

        writer.Write(types.IndexOf(type));
    }

    public static Type ReadType(BinaryReader reader)
    {
        int index = reader.ReadInt32();

        return types[index];
    }

    public static byte[] Serialize<T>(T message)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            WriteType(writer, typeof(T));

            SerializeObject(writer, message, typeof(T));

            return stream.ToArray();
        }
    }

    public static object Deserialize(byte[] data)
    {
        using (var stream = new MemoryStream(data))
        using (var reader = new BinaryReader(stream))
        {
            return DeserializeObject(reader, ReadType(reader));
        }
    }

    public static void SerializeArray(BinaryWriter writer,
                                        IList array)
    {
        WriteType(writer, array.GetType().GetElementType());
        writer.Write(array.Count);

        foreach (object element in array)
        {
            SerializeValue(writer, element);
        }
    }

    public static IList DeserializeArray(BinaryReader reader)
    {
        Type type = ReadType(reader);

        int length = reader.ReadInt32();
        var array = System.Array.CreateInstance(type, length);

        for (int i = 0; i < length; ++i)
        {
            array.SetValue(DeserializeValue(reader), i);
        }

        return array; 
    }

    public static void SerializeValue(BinaryWriter writer, 
                                        object value)
    {
        Type type = value != null ? value.GetType() : typeof(Null);

        if (type.IsArray && !overrides.ContainsKey(type))
        {
            WriteType(writer, typeof(Array));
            SerializeArray(writer, (IList) value);
        }
        else
        {
            WriteType(writer, type);
            overrides[type].serialize(writer, value);
        }
    }

    public static object DeserializeValue(BinaryReader reader)
    {
        Type type = ReadType(reader);

        object value;
            
        if (type == typeof(Array))
        {
            value = DeserializeArray(reader);
        }
        else
        {
            value = overrides[type].deserialize(reader);
        }

        return value;
    }

    public static void SerializeObject(BinaryWriter writer, 
                                        object @object, 
                                        Type type)
    {
        foreach (FieldInfo field in type.GetFields())
        {
            SerializeValue(writer, field.GetValue(@object));
        }
    }

    public static object DeserializeObject(BinaryReader reader, Type type)
    {
        object @object = Activator.CreateInstance(type);

        foreach (FieldInfo field in type.GetFields())
        {
            field.SetValue(@object, DeserializeValue(reader));
        }

        return @object;
    }

    #endregion
}
