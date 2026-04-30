using Microsoft.Xna.Framework;

namespace MiniGolf.Physics.Colliders;

/// <summary>
/// Axis-aligned bounding-box collider for static walls and obstacles.
/// Position is the top-left corner; <see cref="Size"/> is width/height in pixels.
/// </summary>
public sealed class AabbCollider : ICollider
{
    /// <summary>Top-left corner in world pixels.</summary>
    public Vector2 Position;

    /// <summary>Width and height in world pixels.</summary>
    public Vector2 Size;

    public AabbCollider(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public AabbCollider(float x, float y, float w, float h)
        : this(new Vector2(x, y), new Vector2(w, h)) { }

    /// <summary>Right edge X coordinate.</summary>
    public float Right => Position.X + Size.X;

    /// <summary>Bottom edge Y coordinate.</summary>
    public float Bottom => Position.Y + Size.Y;

    /// <inheritdoc />
    public Rectangle Bounds => new(
        (int)Position.X,
        (int)Position.Y,
        (int)Size.X,
        (int)Size.Y);
}
