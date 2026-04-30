using SpaceRace.Physics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// HOOK (bonus feature: stationary gravity sources). A position + strength;
/// each tick, the host iterates active wells and applies an inverse-square
/// force on the ship body via <see cref="ApplyForce"/>. Not added to the
/// physics simulation — it's a force field, not a colliding body. v1 has no
/// wells in the scene.
/// </summary>
public sealed class GravityWell
{
    public NumVector3 Position { get; set; }

    /// <summary>Force coefficient. Effective force at distance d ≈ Strength / d².</summary>
    public float Strength { get; set; } = 200f;

    /// <summary>Wells beyond this distance contribute nothing, to keep the simulation cheap.</summary>
    public float MaxRange { get; set; } = 60f;

    public GravityWell(NumVector3 position, float strength = 200f)
    {
        Position = position;
        Strength = strength;
    }

    /// <summary>Apply inverse-square pull on the ship for one tick.</summary>
    public void ApplyForce(Ship ship, float dt)
    {
        var bodyRef = ship.Body;
        NumVector3 toShip = bodyRef.Pose.Position - Position;
        float dist = toShip.Length();
        if (dist > MaxRange || dist < 1e-3f) return;

        NumVector3 direction = toShip / dist;
        float forceMagnitude = Strength / (dist * dist);
        NumVector3 impulse = -direction * forceMagnitude * ship.Mass * dt;
        bodyRef.ApplyImpulse(impulse, NumVector3.Zero);
        bodyRef.Awake = true;
    }
}
