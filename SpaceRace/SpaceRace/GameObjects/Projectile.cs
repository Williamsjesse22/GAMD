using BepuPhysics;
using BepuPhysics.Collidables;
using SpaceRace.Physics;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// HOOK (bonus feature: lasers / torpedoes). A small Bepu dynamic body fired
/// from the ship along its forward axis. Has a finite lifetime; on expiry or
/// collision it's removed. v1 disables firing in
/// <see cref="Systems.ShipController.CanFire"/>.
/// </summary>
public sealed class Projectile
{
    public BodyHandle BodyHandle { get; }
    public float RemainingLifetime { get; private set; }

    private readonly BepuWorld _world;

    public Projectile(BepuWorld world, NumVector3 position, NumQuaternion orientation,
        float speed = 60f, float lifetime = 4f)
    {
        _world = world;
        RemainingLifetime = lifetime;

        var sphere = new Sphere(0.15f);
        var inertia = sphere.ComputeInertia(0.1f);
        var bodyDesc = BodyDescription.CreateDynamic(
            new RigidPose(position, orientation),
            inertia,
            new CollidableDescription(world.Simulation.Shapes.Add(sphere), 0.05f),
            new BodyActivityDescription(0.01f));
        BodyHandle = world.Simulation.Bodies.Add(bodyDesc);

        // Forward in ship-local convention is -Z; rotate by ship orientation to get world dir.
        NumVector3 forward = NumVector3.Transform(new NumVector3(0f, 0f, -1f), orientation);
        var bodyRef = world.Simulation.Bodies[BodyHandle];
        bodyRef.Velocity.Linear = forward * speed;
    }

    public void Tick(float dt) => RemainingLifetime -= dt;
    public bool IsExpired => RemainingLifetime <= 0f;

    public void Remove() => _world.Simulation.Bodies.Remove(BodyHandle);
}
