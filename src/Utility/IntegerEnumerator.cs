namespace Tactician.Utility;

public ref struct IntegerEnumerator {
    private readonly int _end;
    private readonly int _increment;

    public IntegerEnumerator GetEnumerator() {
        return this;
    }

    public IntegerEnumerator(int start, int end) {
        Current = start;
        _end = end;
        if (end >= start)
            _increment = 1;
        else if (end < start)
            _increment = -1;
        else
            _increment = 0;
    }

    // does not include a, but does include b.
    public static IntegerEnumerator IntegersBetween(int a, int b) {
        return new IntegerEnumerator(a, b);
    }

    public bool MoveNext() {
        Current += _increment;
        return (_increment > 0 && Current <= _end) || (_increment < 0 && Current >= _end);
    }

    public int Current { get; private set; }
}