using Microsoft.Xna.Framework;

namespace MiniGolf.Physics;

/// <summary>
/// Rectangular world region that contributes acceleration to any body whose
/// center lies within it. Two modes:
/// <list type="bullet">
///   <item>Uniform: a single <see cref="Acceleration"/> vector applies anywhere
///         inside the region. Used for simple "everywhere in this patch is
///         downhill east" slopes.</item>
///   <item>Grid: a 2D array of cell vectors (rows × cols) lets the level
///         designer build shaped slopes — e.g. a funnel where every cell points
///         at the hole, or a side-slope whose magnitude varies left-to-right.
///         Each cell renders its own arrow scaled by magnitude.</item>
/// </list>
/// </summary>
public sealed class SlopeRegion
{
    /// <summary>Top-left corner in world pixels.</summary>
    public Vector2 Position;

    /// <summary>Width and height in world pixels.</summary>
    public Vector2 Size;

    /// <summary>Acceleration applied when <see cref="Cells"/> is null. Pixels/second^2.</summary>
    public Vector2 Acceleration;

    /// <summary>
    /// Optional per-cell acceleration grid indexed [row, col] (row-major,
    /// row 0 is the top of the region). Each entry is direction × magnitude.
    /// When non-null, supersedes <see cref="Acceleration"/>.
    /// </summary>
    public Vector2[,]? Cells;

    /// <summary>Tint used for HUD/scene rendering. Red = uphill, green = downhill.</summary>
    public Color Tint;

    /// <summary>Optional uppercase label drawn centered in the region (e.g. "UPHILL").</summary>
    public string? Label;

    public SlopeRegion(Vector2 position, Vector2 size, Vector2 acceleration, Color tint, string? label = null)
    {
        Position = position;
        Size = size;
        Acceleration = acceleration;
        Tint = tint;
        Label = label;
    }

    /// <summary>Returns true if the world point lies inside this region.</summary>
    public bool Contains(Vector2 point)
    {
        return point.X >= Position.X
            && point.X <= Position.X + Size.X
            && point.Y >= Position.Y
            && point.Y <= Position.Y + Size.Y;
    }

    /// <summary>
    /// Returns the acceleration vector applied at <paramref name="point"/>.
    /// Uses the cell grid when present, otherwise falls back to the uniform value.
    /// </summary>
    public Vector2 GetAccelerationAt(Vector2 point)
    {
        if (Cells is null) return Acceleration;

        int rows = Cells.GetLength(0);
        int cols = Cells.GetLength(1);
        if (rows == 0 || cols == 0) return Acceleration;

        // Map point to normalized [0,1) cell coords. Clamp slightly under 1 so
        // points exactly on the right/bottom edge don't index out of bounds.
        float u = MathHelper.Clamp((point.X - Position.X) / Size.X, 0f, 0.999999f);
        float v = MathHelper.Clamp((point.Y - Position.Y) / Size.Y, 0f, 0.999999f);
        int col = (int)(u * cols);
        int row = (int)(v * rows);
        return Cells[row, col];
    }
}
