namespace Tactician.Components;

public readonly record struct Timer(float Time, float Max) {
    public Timer(float time) : this(time, time) {
    }

    public float Remaining => Time / Max;
}