using System;
using Microsoft.Xna.Framework;

namespace SpaceRace.Physics;

/// <summary>
/// Geometric pass-through volume used by rings, pitstops, fuel pickups, and the
/// finish line. Tests whether a body's previous-frame and current-frame
/// positions cross a circular disc (defined by center, axis, inner radius)
/// during the tick.
/// </summary>
/// <remarks>
/// Uses parametric segment-vs-plane intersection (<c>t = -d0 / (d1 - d0)</c>)
/// rather than a per-frame distance check, which would tunnel for fast bodies.
/// </remarks>
public sealed class Trigger
{
    /// <summary>World-space center of the disc.</summary>
    public Vector3 Center;

    /// <summary>Unit normal of the disc's plane (oriented along the ring's tube axis).</summary>
    public Vector3 Axis;

    /// <summary>Radius of the open hole (must be < ring's major radius).</summary>
    public float InnerRadius;

    public Trigger(Vector3 center, Vector3 axis, float innerRadius)
    {
        Center = center;
        Axis = Vector3.Normalize(axis);
        InnerRadius = innerRadius;
    }

    /// <summary>
    /// Did the segment <paramref name="prevPos"/> → <paramref name="currPos"/> cross
    /// this disc within the inner radius?
    /// </summary>
    /// <param name="passDirection">+1 if crossed in +Axis direction; -1 if -Axis; 0 if no cross.</param>
    public bool TryPassThrough(Vector3 prevPos, Vector3 currPos, out int passDirection)
    {
        passDirection = 0;
        float d0 = Vector3.Dot(prevPos - Center, Axis);
        float d1 = Vector3.Dot(currPos - Center, Axis);

        // Same side of plane (no sign change) — no crossing.
        if (d0 * d1 > 0f) return false;
        // Both very close to zero (or identical) — no meaningful crossing.
        if (MathF.Abs(d1 - d0) < 1e-6f) return false;

        float t = -d0 / (d1 - d0);
        if (t < 0f || t > 1f) return false; // numerical guard

        Vector3 hitPoint = Vector3.Lerp(prevPos, currPos, t);
        Vector3 toPoint = hitPoint - Center;
        Vector3 onPlane = toPoint - Vector3.Dot(toPoint, Axis) * Axis;
        if (onPlane.LengthSquared() > InnerRadius * InnerRadius) return false;

        // d0 > 0 means we started on the +Axis side and moved to -Axis side.
        passDirection = d0 > 0f ? -1 : 1;
        return true;
    }
}
