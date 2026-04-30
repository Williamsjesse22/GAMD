using Microsoft.Xna.Framework;

namespace MiniGolf.Physics;

/// <summary>
/// State for a 2D rigid body integrated by <see cref="PhysicsWorld"/>.
/// Position is in world pixels, velocity in pixels/second, mass in arbitrary units.
/// LinearDamping is a per-second drag coefficient applied each tick (rolling friction analogue).
/// </summary>
public sealed class RigidBody
{
    /// <summary>Center position in world space, pixels.</summary>
    public Vector2 Position;

    /// <summary>Linear velocity, pixels per second.</summary>
    public Vector2 Velocity;

    /// <summary>Mass; only matters relative to other masses for impulse responses.</summary>
    public float Mass = 1f;

    /// <summary>Per-second linear damping. Applied as v *= (1 - LinearDamping * dt) per tick.</summary>
    public float LinearDamping = 0.6f;

    /// <summary>Coefficient of restitution used on collision response (1.0 = perfect bounce).</summary>
    public float Restitution = 0.78f;

    /// <summary>Accumulator for accelerations contributed during a tick (slope regions, etc.).</summary>
    public Vector2 AccumulatedAcceleration;

    /// <summary>True when |Velocity| is below the world stop epsilon and the body has been snapped.</summary>
    public bool IsAtRest => Velocity.LengthSquared() == 0f;

    /// <summary>Apply an instantaneous velocity change (impulse / mass).</summary>
    /// <param name="impulse">Impulse vector in pixels/second * mass.</param>
    public void ApplyImpulse(Vector2 impulse)
    {
        Velocity += impulse / Mass;
    }
}
