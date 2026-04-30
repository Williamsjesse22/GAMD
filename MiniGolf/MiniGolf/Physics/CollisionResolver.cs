using System;
using Microsoft.Xna.Framework;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.Physics;

/// <summary>
/// Static collision detection and response for the supported shape pairs.
/// Resolution is positional (push the ball out of penetration) plus reflective
/// (mirror velocity about the contact normal, scaled by restitution).
/// </summary>
public static class CollisionResolver
{
    /// <summary>Information about a resolved contact, returned to callers that want to react (sound, etc.).</summary>
    public readonly struct Contact
    {
        public readonly bool Hit;
        public readonly Vector2 Normal;
        public readonly float Penetration;

        public Contact(bool hit, Vector2 normal, float penetration)
        {
            Hit = hit;
            Normal = normal;
            Penetration = penetration;
        }

        public static readonly Contact None = new(false, Vector2.Zero, 0f);
    }

    /// <summary>
    /// Resolves a circle vs an axis-aligned box. Mutates the circle's body position
    /// and velocity; the AABB is treated as static.
    /// </summary>
    public static Contact ResolveCircleAabb(CircleCollider circle, AabbCollider aabb)
    {
        // Closest point on box to circle center.
        Vector2 c = circle.Body.Position;
        float cx = MathHelper.Clamp(c.X, aabb.Position.X, aabb.Right);
        float cy = MathHelper.Clamp(c.Y, aabb.Position.Y, aabb.Bottom);

        Vector2 closest = new(cx, cy);
        Vector2 delta = c - closest;
        float distSq = delta.LengthSquared();
        float r = circle.Radius;

        if (distSq > r * r) return Contact.None;

        Vector2 normal;
        float penetration;

        if (distSq > 1e-6f)
        {
            float dist = MathF.Sqrt(distSq);
            normal = delta / dist;
            penetration = r - dist;
        }
        else
        {
            // Center is inside the box — pick the shallowest axis to push out along.
            float dxLeft = c.X - aabb.Position.X;
            float dxRight = aabb.Right - c.X;
            float dyTop = c.Y - aabb.Position.Y;
            float dyBottom = aabb.Bottom - c.Y;

            float minX = MathF.Min(dxLeft, dxRight);
            float minY = MathF.Min(dyTop, dyBottom);

            if (minX < minY)
            {
                normal = dxLeft < dxRight ? new Vector2(-1, 0) : new Vector2(1, 0);
                penetration = minX + r;
            }
            else
            {
                normal = dyTop < dyBottom ? new Vector2(0, -1) : new Vector2(0, 1);
                penetration = minY + r;
            }
        }

        // Positional correction: push the circle out along the normal.
        circle.Body.Position += normal * penetration;

        // Reflect velocity about the normal, scaled by restitution. Only reflect
        // if the velocity is heading into the surface, otherwise the ball can
        // jitter when resting against a wall.
        float vn = Vector2.Dot(circle.Body.Velocity, normal);
        if (vn < 0f)
        {
            circle.Body.Velocity -= (1f + circle.Body.Restitution) * vn * normal;
        }

        return new Contact(true, normal, penetration);
    }
}
