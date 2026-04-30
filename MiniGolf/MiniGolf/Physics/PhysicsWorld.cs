using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.Physics;

/// <summary>
/// Owns the dynamic body (the ball), all static colliders (walls, obstacles), and
/// slope regions. Steps the simulation once per tick at a fixed dt.
/// </summary>
public sealed class PhysicsWorld
{
    /// <summary>Below this speed (px/s) the ball is snapped to rest.</summary>
    public float StopEpsilon { get; set; } = 8f;

    /// <summary>Single dynamic body (the ball). One ball per game.</summary>
    public CircleCollider? Ball { get; set; }

    /// <summary>Static box colliders (outer walls, dividers, obstacles).</summary>
    public List<AabbCollider> StaticBoxes { get; } = new();

    /// <summary>Slope regions contributing constant acceleration to bodies that overlap them.</summary>
    public List<SlopeRegion> Slopes { get; } = new();

    /// <summary>Raised once per resolved circle-vs-box contact (use for bounce SFX).</summary>
    public event System.Action? BallBounced;

    /// <summary>Raised the first frame the ball reaches rest after being in motion.</summary>
    public event System.Action? BallStopped;

    private bool _wasMoving;

    /// <summary>
    /// Advance the simulation by <paramref name="dt"/> seconds. Order per tick:
    /// (1) sum slope acceleration, (2) integrate velocity with damping,
    /// (3) integrate position, (4) resolve collisions, (5) snap to rest if slow.
    /// </summary>
    public void Step(float dt)
    {
        if (Ball is null) return;

        var body = Ball.Body;

        // 1. Sum accelerations from slope regions the ball overlaps.
        // GetAccelerationAt handles both uniform and grid-cell slopes.
        body.AccumulatedAcceleration = Vector2.Zero;
        for (int i = 0; i < Slopes.Count; i++)
        {
            if (Slopes[i].Contains(body.Position))
                body.AccumulatedAcceleration += Slopes[i].GetAccelerationAt(body.Position);
        }

        // 2. Integrate velocity with damping.
        body.Velocity = (body.Velocity + body.AccumulatedAcceleration * dt) *
                        (1f - body.LinearDamping * dt);

        // 3. Integrate position.
        body.Position += body.Velocity * dt;

        // 4. Resolve collisions against all static boxes.
        for (int i = 0; i < StaticBoxes.Count; i++)
        {
            var contact = CollisionResolver.ResolveCircleAabb(Ball, StaticBoxes[i]);
            if (contact.Hit) BallBounced?.Invoke();
        }

        // 5. Snap to rest below epsilon to allow the next shot.
        bool isMoving = body.Velocity.LengthSquared() > StopEpsilon * StopEpsilon;
        if (!isMoving) body.Velocity = Vector2.Zero;

        if (_wasMoving && !isMoving) BallStopped?.Invoke();
        _wasMoving = isMoving;
    }
}
