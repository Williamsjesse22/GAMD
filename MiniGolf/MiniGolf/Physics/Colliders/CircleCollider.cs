using Microsoft.Xna.Framework;

namespace MiniGolf.Physics.Colliders;

/// <summary>
/// Circle collider; position is the body's center. Used for the ball.
/// </summary>
public sealed class CircleCollider : ICollider
{
    /// <summary>The rigid body this collider tracks.</summary>
    public RigidBody Body { get; }

    /// <summary>Circle radius in world pixels.</summary>
    public float Radius { get; set; }

    public CircleCollider(RigidBody body, float radius)
    {
        Body = body;
        Radius = radius;
    }

    /// <inheritdoc />
    public Rectangle Bounds
    {
        get
        {
            int r = (int)System.MathF.Ceiling(Radius);
            return new Rectangle(
                (int)(Body.Position.X - r),
                (int)(Body.Position.Y - r),
                r * 2,
                r * 2);
        }
    }
}
