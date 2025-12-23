using System;
using System.Collections.Generic;

namespace Tactician.Data;

// We can't store strings in ECS because they are managed types!
public static class TextStorage {
    private static readonly Dictionary<string, int> StringToID = new();
    private static readonly Stack<int> OpenIDs = new();
    private static string[] _idToString = new string[256];
    private static int _nextId;

    // TODO: is there some way we can reliably clear strings to free memory?

    public static string GetString(int id) {
        return _idToString[id];
    }

    public static int GetID(string text) {
        if (!StringToID.ContainsKey(text)) RegisterString(text);

        return StringToID[text];
    }

    private static void RegisterString(string text) {
        if (OpenIDs.Count == 0) {
            if (_nextId >= _idToString.Length) Array.Resize(ref _idToString, _idToString.Length * 2);
            StringToID[text] = _nextId;
            _idToString[_nextId] = text;
            _nextId += 1;
        }
        else {
            StringToID[text] = OpenIDs.Pop();
        }
    }
}