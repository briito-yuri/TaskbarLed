namespace TaskbarLed.Core.Visibility;

/// <summary>Screen rectangle in physical pixels; <see cref="Right"/>/<see cref="Bottom"/> are exclusive.</summary>
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Math.Max(0, Right - Left);

    public int Height => Math.Max(0, Bottom - Top);

    public bool IsEmpty => Width == 0 || Height == 0;

    public long Area => (long)Width * Height;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public PixelRect Intersect(PixelRect other) => new(
        Math.Max(Left, other.Left),
        Math.Max(Top, other.Top),
        Math.Min(Right, other.Right),
        Math.Min(Bottom, other.Bottom));

    public static PixelRect FromSize(int left, int top, int width, int height) => new(left, top, left + width, top + height);
}
