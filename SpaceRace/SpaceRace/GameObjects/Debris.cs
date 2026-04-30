using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceRace.Graphics;
using SpaceRace.Physics;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// A drifting space rock — Bepu dynamic body + a procedural sphere mesh.
/// <see cref="DebrisSpawner"/> creates these and removes them when their
/// lifetime expires. Collides physically with the ship.
/// </summary>
public sealed class Debris : DrawableGameComponent
{
    private readonly BepuWorld _world;
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly Color _color;

    /// <summary>Bepu dynamic body handle.</summary>
    public BodyHandle BodyHandle { get; }

    /// <summary>Seconds until this debris should be removed.</summary>
    public float RemainingLifetime { get; private set; }

    /// <summary>True once <see cref="RemainingLifetime"/> hits 0.</summary>
    public bool IsExpired => RemainingLifetime <= 0f;

    public Debris(Game game, BepuWorld world, PrimitiveRenderer renderer,
        NumVector3 spawnPosition, NumVector3 initialVelocity,
        float radius, float lifetime, Color color) : base(game)
    {
        _world = world;
        _renderer = renderer;
        _color = color;
        RemainingLifetime = lifetime;
        DrawOrder = 5;

        var sphere = new Sphere(radius);
        var inertia = sphere.ComputeInertia(radius * radius * 4f);
        var bodyDesc = BodyDescription.CreateDynamic(
            new RigidPose(spawnPosition, NumQuaternion.Identity),
            inertia,
            new CollidableDescription(world.Simulation.Shapes.Add(sphere), 0.1f),
            new BodyActivityDescription(0.01f));
        BodyHandle = world.Simulation.Bodies.Add(bodyDesc);
        var bodyRef = world.Simulation.Bodies[BodyHandle];
        bodyRef.Velocity.Linear = initialVelocity;

        var mesh = MeshFactory.CreateSphere(radius, 8, 12);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        RemainingLifetime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime)
    {
        var pose = _world.Simulation.Bodies[BodyHandle].Pose;
        _renderer.DrawMesh(_vb, _ib, pose.ToWorldMatrix(), _color);
    }

    /// <summary>Remove the body from the simulation. Caller is responsible for removing the component.</summary>
    public void RemoveFromSimulation() => _world.Simulation.Bodies.Remove(BodyHandle);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vb.Dispose();
            _ib.Dispose();
        }
        base.Dispose(disposing);
    }
}
