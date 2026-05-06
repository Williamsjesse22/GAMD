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
/// Small fast Bepu dynamic body fired from the ship's nose. Auto-expires after
/// <see cref="RemainingLifetime"/> seconds. Rendered as a glowing yellow sphere.
/// <see cref="ProjectileManager"/> owns spawning and collision-vs-debris.
/// </summary>
public sealed class Projectile : DrawableGameComponent
{
    private readonly BepuWorld _world;
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;

    public BodyHandle BodyHandle { get; }
    public float RemainingLifetime { get; private set; }
    public bool IsExpired => RemainingLifetime <= 0f;
    public bool IsConsumed { get; private set; }
    public NumVector3 Position => _world.Simulation.Bodies[BodyHandle].Pose.Position;

    public Projectile(Game game, BepuWorld world, PrimitiveRenderer renderer,
        NumVector3 spawnPos, NumVector3 velocity, float lifetime = 3f) : base(game)
    {
        _world = world;
        _renderer = renderer;
        RemainingLifetime = lifetime;
        DrawOrder = 6;

        const float radius = 0.18f;
        var sphere = new Sphere(radius);
        var inertia = sphere.ComputeInertia(0.05f);
        var bodyDesc = BodyDescription.CreateDynamic(
            new RigidPose(spawnPos, NumQuaternion.Identity),
            inertia,
            new CollidableDescription(world.Simulation.Shapes.Add(sphere), 0.05f),
            new BodyActivityDescription(0.01f));
        BodyHandle = world.Simulation.Bodies.Add(bodyDesc);
        var bodyRef = world.Simulation.Bodies[BodyHandle];
        bodyRef.Velocity.Linear = velocity;

        var mesh = MeshFactory.CreateSphere(radius * 1.4f, 6, 8);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        RemainingLifetime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime)
    {
        var pose = _world.Simulation.Bodies[BodyHandle].Pose;
        Matrix world = pose.ToWorldMatrix();
        _renderer.DrawMesh(_vb, _ib, world, new Color(255, 200, 50));
        _renderer.DrawGlow(_vb, _ib, world, new Color(255, 230, 100), scale: 1.6f);
    }

    /// <summary>Mark this projectile for removal after a hit.</summary>
    public void Consume() => IsConsumed = true;

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
