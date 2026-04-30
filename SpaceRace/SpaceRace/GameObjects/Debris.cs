using BepuPhysics;
using BepuPhysics.Collidables;
using SpaceRace.Physics;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// HOOK (bonus feature: space debris). A drifting Bepu dynamic body with a
/// lifetime; <see cref="DebrisSpawner"/> creates and despawns these. Renders as
/// a small sphere; collides physically with the ship via standard Bepu narrow
/// phase. Not spawned in v1.
/// </summary>
public sealed class Debris
{
    /// <summary>Bepu dynamic body handle.</summary>
    public BodyHandle BodyHandle { get; }

    /// <summary>Seconds until this debris should be removed.</summary>
    public float RemainingLifetime { get; private set; }

    private readonly BepuWorld _world;

    public Debris(BepuWorld world, NumVector3 spawnPosition, NumVector3 initialVelocity,
        float radius = 0.5f, float lifetime = 30f)
    {
        _world = world;
        RemainingLifetime = lifetime;

        var sphere = new Sphere(radius);
        var inertia = sphere.ComputeInertia(radius * radius * 4f);
        var bodyDesc = BodyDescription.CreateDynamic(
            new RigidPose(spawnPosition, NumQuaternion.Identity),
            inertia,
            new CollidableDescription(world.Simulation.Shapes.Add(sphere), 0.1f),
            new BodyActivityDescription(0.01f));
        BodyHandle = world.Simulation.Bodies.Add(bodyDesc);

        // Set initial velocity post-add to avoid coupling to V2 BodyVelocity ctor variants.
        var bodyRef = world.Simulation.Bodies[BodyHandle];
        bodyRef.Velocity.Linear = initialVelocity;
    }

    public void Tick(float dt) => RemainingLifetime -= dt;
    public bool IsExpired => RemainingLifetime <= 0f;

    public void Remove() => _world.Simulation.Bodies.Remove(BodyHandle);
}
