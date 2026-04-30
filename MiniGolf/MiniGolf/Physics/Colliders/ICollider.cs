using Microsoft.Xna.Framework;

namespace MiniGolf.Physics.Colliders;

/// <summary>
/// Marker interface for collision shapes. Concrete shapes are dispatched on by
/// <see cref="CollisionResolver"/>; we avoid double-dispatch / visitor patterns
/// to keep allocation pressure off the physics hot path.
/// </summary>
public interface ICollider
{
    /// <summary>Axis-aligned bounding rectangle for broad-phase culling.</summary>
    Rectangle Bounds { get; }
}
