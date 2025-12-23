using System;
using System.Collections.Generic;
using System.Numerics;

namespace Tactician.Utility;

public static class Rando {
    private static Random Rand { get; } = new();

    public static float Value => Rand.NextSingle();

    public static Quaternion Orientation =>
        Quaternion.CreateFromYawPitchRoll(
            Value * MathF.PI * 2,
            Value * MathF.PI * 2,
            Value * MathF.PI * 2
        );

    public static float Range(float min, float max) {
        return Value * (max - min) + min;
    }

    public static void Shuffle<T>(this T[] arr) {
        for (var i = arr.Length - 1; i >= 1; i--) {
            var j = IntInclusive(0, i);
            (arr[j], arr[i]) = (arr[i], arr[j]);
        }
    }

    public static int Int(int min, int max) {
        return Rand.Next(min, max);
    }

    public static int IntInclusive(int min, int max) {
        return Rand.Next(min, max + 1);
    }

    public static Vector2 OnUnitCircle() {
        var theta = Value * MathF.PI * 2.0f;
        return new Vector2(MathF.Cos(theta), MathF.Sin(theta));
    }

    public static Vector3 InsideUnitSphere() {
        var theta = Value * MathF.PI * 2.0f;
        var phi = MathF.PI * Value;
        var r = Value;
        return new Vector3(
            MathF.Sin(phi) * MathF.Cos(theta),
            MathF.Sin(phi) * MathF.Sin(theta),
            MathF.Cos(phi)
        ) * r;
    }

    public static Vector3 OnUnitSphere() {
        return Vector3.Normalize(InsideUnitSphere());
    }

    public static float GetRandomQuarterTurn() {
        var theta = Value;
        if (theta < 0.25) return 0;

        if (theta < 0.5) return MathF.PI * 0.5f;

        if (theta < 0.75) return MathF.PI;

        return MathF.PI * 0.75f;
    }

    public static T GetRandomItem<T>(this List<T> list) {
        var index = Int(0, list.Count);
        return list[index];
    }

    public static T GetRandomItem<T>(this T[] arr, float value) {
        var index = (int)MathF.Floor(value * arr.Length);
        return arr[index];
    }

    public static T GetRandomItem<T>(this T[] arr) {
        var index = Int(0, arr.Length);
        return arr[index];
    }

    public static T GetRandomItem<T>(this Array arr) {
        var index = Int(0, arr.Length);
        return (T)arr.GetValue(index);
    }

    public static T GetRandomItemWeighted<T>(this SortedDictionary<int, T[]> dict) {
        var total = 0;
        foreach (var key in dict.Keys) total += key;

        var n = Int(0, total);

        var keys = new List<int>(dict.Keys);

        var index = keys.BinarySearch(0, dict.Count, n, null);
        if (index < 0) index = ~index;

        var choice = keys[index % keys.Count];

        return dict[choice].GetRandomItem();
    }

    public static T GetRandomItemWeighted<T>(this SortedDictionary<int, T[]> dict, float value) {
        var total = 0;
        foreach (var key in dict.Keys) total += key;

        var n = (int)MathF.Floor(Value * total);

        var keys = new List<int>(dict.Keys);

        var index = keys.BinarySearch(0, dict.Count, n, null);
        if (index < 0) index = ~index;

        var choice = keys[index];

        return dict[choice].GetRandomItem(value);
    }
}