using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tactician.Components;

namespace Tactician.Utility;

/// <summary>
///     Used to quickly check if two shapes are potentially overlapping.
/// </summary>
/// <typeparam name="T">The type that will be used to uniquely identify shape-transform pairs.</typeparam>
public sealed class SpatialHash<T> where T : unmanaged, IEquatable<T> {
    private readonly List<T>[][] _cells;
    private readonly int _cellSize;
    private readonly int _columnCount;

    private readonly Queue<HashSet<T>> _hashSetPool = new();
    private readonly int _height;
    private readonly Dictionary<T, Rectangle> _idBoxLookup = new();
    private readonly int _rowCount;
    private readonly int _width;

    private readonly int _x;
    private readonly int _y;

    public SpatialHash(int x, int y, int width, int height, int cellSize) {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
        _rowCount = width / cellSize;
        _columnCount = height / cellSize;
        _cellSize = cellSize;

        _cells = new List<T>[_rowCount][];
        for (var i = 0; i < _rowCount; i += 1) {
            _cells[i] = new List<T>[_columnCount];

            for (var j = 0; j < _columnCount; j += 1) _cells[i][j] = new List<T>();
        }
    }

    private (int, int) Hash(int x, int y) {
        return (x / _cellSize, y / _cellSize);
    }

    // TODO: we could speed this up with a proper Update check
    // that checks the difference between the two hash key ranges

    /// <summary>
    ///     Inserts an element into the SpatialHash.
    ///     Rectangles outside of the hash range will be ignored!
    /// </summary>
    /// <param name="id">A unique ID for the shape-transform pair.</param>
    public void Insert(T id, Rectangle rectangle) {
        var relativeX = rectangle.X - _x;
        var relativeY = rectangle.Y - _y;
        var rowRangeStart = Math.Clamp(relativeX / _cellSize, 0, _rowCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / _cellSize, 0, _rowCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / _cellSize, 0, _columnCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / _cellSize, 0, _columnCount - 1);

        for (var i = rowRangeStart; i <= rowRangeEnd; i += 1)
        for (var j = columnRangeStart; j <= columnRangeEnd; j += 1)
            _cells[i][j].Add(id);

        _idBoxLookup[id] = rectangle;
    }

    /// <summary>
    ///     Retrieves all the potential collisions of a shape-transform pair. Excludes any shape-transforms with the given ID.
    /// </summary>
    public RetrieveEnumerator Retrieve(T id, Rectangle rectangle) {
        var relativeX = rectangle.X - _x;
        var relativeY = rectangle.Y - _y;
        var rowRangeStart = Math.Clamp(relativeX / _cellSize, 0, _rowCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / _cellSize, 0, _rowCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / _cellSize, 0, _columnCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / _cellSize, 0, _columnCount - 1);

        return new RetrieveEnumerator(
            this,
            Keys(rowRangeStart, columnRangeStart, rowRangeEnd, columnRangeEnd),
            id
        );
    }

    /// <summary>
    ///     Retrieves objects based on a pre-transformed AABB.
    /// </summary>
    /// <param name="aabb">A transformed AABB.</param>
    /// <returns></returns>
    public RetrieveEnumerator Retrieve(Rectangle rectangle) {
        var relativeX = rectangle.X - _x;
        var relativeY = rectangle.Y - _y;
        var rowRangeStart = Math.Clamp(relativeX / _cellSize, 0, _rowCount - 1);
        var rowRangeEnd = Math.Clamp((relativeX + rectangle.Width) / _cellSize, 0, _rowCount - 1);
        var columnRangeStart = Math.Clamp(relativeY / _cellSize, 0, _columnCount - 1);
        var columnRangeEnd = Math.Clamp((relativeY + rectangle.Height) / _cellSize, 0, _columnCount - 1);

        return new RetrieveEnumerator(
            this,
            Keys(rowRangeStart, columnRangeStart, rowRangeEnd, columnRangeEnd)
        );
    }

    /// <summary>
    ///     Removes everything that has been inserted into the SpatialHash.
    /// </summary>
    public void Clear() {
        for (var i = 0; i < _rowCount; i += 1)
        for (var j = 0; j < _columnCount; j += 1)
            _cells[i][j].Clear();

        _idBoxLookup.Clear();
    }

    internal static KeysEnumerator Keys(int minX, int minY, int maxX, int maxY) {
        return new KeysEnumerator(minX, minY, maxX, maxY);
    }

    private HashSet<T> AcquireHashSet() {
        if (_hashSetPool.Count == 0) _hashSetPool.Enqueue(new HashSet<T>());

        var hashSet = _hashSetPool.Dequeue();
        hashSet.Clear();
        return hashSet;
    }

    private void FreeHashSet(HashSet<T> hashSet) {
        _hashSetPool.Enqueue(hashSet);
    }

    internal ref struct KeysEnumerator {
        private int _minX;
        private readonly int _minY;
        private readonly int _maxX;
        private readonly int _maxY;
        private int _i, _j;

        public KeysEnumerator GetEnumerator() {
            return this;
        }

        public KeysEnumerator(int minX, int minY, int maxX, int maxY) {
            _minX = minX;
            _minY = minY;
            _maxX = maxX;
            _maxY = maxY;
            _i = minX;
            _j = minY - 1;
        }

        public bool MoveNext() {
            if (_j < _maxY) {
                _j += 1;
                return true;
            }

            if (_i < _maxX) {
                _i += 1;
                _j = _minY;
                return true;
            }

            return false;
        }

        public (int, int) Current => (_i, _j);
    }

    public ref struct RetrieveEnumerator {
        public readonly SpatialHash<T> SpatialHash;
        private KeysEnumerator _keysEnumerator;
        private Span<T>.Enumerator _spanEnumerator;
        private bool _hashSetEnumeratorActive;
        private readonly HashSet<T> _duplicates;
        private readonly T? _id;

        public RetrieveEnumerator GetEnumerator() {
            return this;
        }

        internal RetrieveEnumerator(
            SpatialHash<T> spatialHash,
            KeysEnumerator keysEnumerator,
            T id
        ) {
            SpatialHash = spatialHash;
            _keysEnumerator = keysEnumerator;
            _spanEnumerator = default;
            _hashSetEnumeratorActive = false;
            _duplicates = SpatialHash.AcquireHashSet();
            _id = id;
        }

        internal RetrieveEnumerator(
            SpatialHash<T> spatialHash,
            KeysEnumerator keysEnumerator
        ) {
            SpatialHash = spatialHash;
            _keysEnumerator = keysEnumerator;
            _spanEnumerator = default;
            _hashSetEnumeratorActive = false;
            _duplicates = SpatialHash.AcquireHashSet();
            _id = null;
        }

        public bool MoveNext() {
            if (!_hashSetEnumeratorActive || !_spanEnumerator.MoveNext()) {
                if (!_keysEnumerator.MoveNext()) return false;

                var (i, j) = _keysEnumerator.Current;
                _spanEnumerator = CollectionsMarshal.AsSpan(SpatialHash._cells[i][j]).GetEnumerator();
                _hashSetEnumeratorActive = true;

                return MoveNext();
            }

            // conditions
            var t = _spanEnumerator.Current;

            if (_duplicates.Contains(t)) return MoveNext();

            if (_id.HasValue)
                if (_id.Value.Equals(t))
                    return MoveNext();

            _duplicates.Add(t);
            return true;
        }

        public (T, Rectangle) Current {
            get {
                var t = _spanEnumerator.Current;
                var rect = SpatialHash._idBoxLookup[t];
                return (t, rect);
            }
        }

        public void Dispose() {
            SpatialHash.FreeHashSet(_duplicates);
        }
    }
}