﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Profiler = UnityEngine.Profiling.Profiler;

public static class JSON
{
    public static JsonSerializerSettings settings;
    
    static JSON()
    {
        settings = new JsonSerializerSettings();
        settings.TypeNameHandling = TypeNameHandling.Auto;
        settings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        //settings.Converters.Add(new Vector2Converter());
        settings.Converters.Add(new RectConverter());
        settings.Converters.Add(new ColorConverter());
        settings.Converters.Add(new ByteSetConverter());
        settings.Converters.Add(new IntVector2Converter());
        settings.Converters.Add(new IntRectConverter());
        settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    }

    public static T Deserialise<T>(string value)
    {
        T returnObject = JsonConvert.DeserializeObject<T>(value, settings);

        return returnObject;
    }

    public static string Serialise<T>(T obj, bool pretty=false)
    {
        return JsonConvert.SerializeObject(obj, pretty ? Formatting.Indented : Formatting.None, settings);
    }

    public static T Copy<T>(T obj)
    {
        return Deserialise<T>(Serialise(obj));
    }
}

/// <summary>
/// More compact serialization of Dictionaries in JSON.NET
/// </summary>
/// <typeparam name="K">Key</typeparam>
/// <typeparam name="V">Value</typeparam>
/// <typeparam name="T">Type (subclass of Dictionary<K, V>)</typeparam>
public class DictionarySerializer<K, V, T> : JsonConverter
    where T : Dictionary<K, V>, new()
{
    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        var map = value as T;

        writer.WriteStartArray();
        foreach (var pair in map)
        {
            writer.WriteStartArray();
            JToken.FromObject(pair.Key, serializer).WriteTo(writer);
            JToken.FromObject(pair.Value, serializer).WriteTo(writer);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        Profiler.BeginSample("Read Json Dictionary");

        var map = new T();

        var array = JArray.Load(reader);

        foreach (var item in array.Children<JArray>())
        {
            var key = item[0].ToObject<K>(serializer);
            var val = item[1].ToObject<V>(serializer);

            map[key] = val; 
        }

        Profiler.EndSample();

        return map;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }
}

public class Vector2Converter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2);
    }

    public override object ReadJson(JsonReader reader, 
                                    Type objectType, 
                                    object existingValue, 
                                    JsonSerializer serializer)
    {
        var array = JToken.ReadFrom(reader);

        return new Vector2(array[0].Value<float>(),
                           array[1].Value<float>());
    }

    public override void WriteJson(JsonWriter writer, 
                                   object value, 
                                   JsonSerializer serializer)
    {
        Vector2 vector = (Vector2) value;

        writer.WriteStartArray();
        writer.WriteValue(vector.x);
        writer.WriteValue(vector.y);
        writer.WriteEndArray();
    }
}

public class RectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Rect);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        Rect rect = new Rect();

        rect.xMin = (float) reader.ReadAsDecimal().GetValueOrDefault();
        rect.yMin = (float) reader.ReadAsDecimal().GetValueOrDefault();
        rect.xMax = (float) reader.ReadAsDecimal().GetValueOrDefault();
        rect.yMax = (float) reader.ReadAsDecimal().GetValueOrDefault();
        reader.Read();

        return rect;
    }

    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        Rect rect = (Rect) value;

        writer.WriteStartArray();
        writer.WriteValue(rect.xMin);
        writer.WriteValue(rect.yMin);
        writer.WriteValue(rect.xMax);
        writer.WriteValue(rect.yMax);
        writer.WriteEndArray();
    }
}

public class IntVector2Converter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IntVector2);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        var array = JToken.ReadFrom(reader);

        return new IntVector2(array[0].Value<float>(), 
                              array[1].Value<float>());
    }

    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        IntVector2 point = (IntVector2) value;
        
        var values = new JArray(point.x, point.y);

        values.WriteTo(writer);
    }
}

public class IntRectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IntRect);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        var rect = new IntRect();
        var array = JToken.ReadFrom(reader);

        rect.xMin = array[0].Value<int>();
        rect.yMin = array[1].Value<int>();
        rect.xMax = array[2].Value<int>();
        rect.yMax = array[3].Value<int>();

        return rect;
    }

    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        var rect = (IntRect) value;
        var obj = new JArray(rect.xMin, rect.yMin, rect.xMax, rect.yMax);

        obj.WriteTo(writer);
    }
}

public class ColorConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Color)
            || objectType == typeof(Color32);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        Color32 color = new Color32();

        color.r = (byte) reader.ReadAsDecimal().GetValueOrDefault();
        color.g = (byte) reader.ReadAsDecimal().GetValueOrDefault();
        color.b = (byte) reader.ReadAsDecimal().GetValueOrDefault();
        color.a = (byte) reader.ReadAsDecimal().GetValueOrDefault();
        reader.Read();

        if (objectType == typeof(Color)) return (Color) color;

        return color;
    }

    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        Color32 color = default(Color32);
        
        if (value is Color)   color = (Color)   value;
        if (value is Color32) color = (Color32) value;

        writer.WriteStartArray();
        writer.WriteValue(color.r);
        writer.WriteValue(color.g);
        writer.WriteValue(color.b);
        writer.WriteValue(color.a);
        writer.WriteEndArray();
    }
}

public class ByteSetConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(HashSet<byte>);
    }

    public override object ReadJson(JsonReader reader,
                                    Type objectType,
                                    object existingValue,
                                    JsonSerializer serializer)
    {
        var members = serializer.Deserialize<byte[]>(reader);
        
        //reader.Read();

        return new HashSet<byte>(members);
    }

    public override void WriteJson(JsonWriter writer,
                                   object value,
                                   JsonSerializer serializer)
    {
        var set = (HashSet<byte>) value;

        byte[] members = set.ToArray();

        serializer.Serialize(writer, members);
    }
}
