using System;
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
/// The player's spacecraft: a Bepu dynamic body with a wedge-shaped mesh. Holds
/// the body handle that <see cref="Systems.ShipController"/> applies forces to.
/// Forces-only steering — this class never sets position or orientation directly.
/// </summary>
public sealed class Ship : DrawableGameComponent
{
    private readonly BepuWorld _world;
    private readonly PrimitiveRenderer _renderer;
    private readonly Camera _camera;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;

    /// <summary>Handle to the dynamic body in the Bepu simulation.</summary>
    public BodyHandle BodyHandle { get; }

    /// <summary>Mass used when computing impulses from desired accelerations.</summary>
    public float Mass { get; }

    /// <summary>Remaining fuel (0..1). v1 leaves this full; bonus hook for fuel mode.</summary>
    public float Fuel { get; set; } = 1f;

    public Ship(Game game, BepuWorld world, PrimitiveRenderer renderer, Camera camera,
        NumVector3 spawnPosition, float mass = 1f) : base(game)
    {
        _world = world;
        _renderer = renderer;
        _camera = camera;
        Mass = mass;
        DrawOrder = 0;

        // Use a box collider that roughly bounds the wedge: 1.2 wide, 0.6 tall, 2 long.
        var hull = new Box(1.2f, 0.6f, 2.0f);
        var inertia = hull.ComputeInertia(mass);
        var shapeIndex = world.Simulation.Shapes.Add(hull);

        var pose = new RigidPose(spawnPosition, NumQuaternion.Identity);
        var body = BodyDescription.CreateDynamic(
            pose, inertia,
            new CollidableDescription(shapeIndex, 0.1f),
            new BodyActivityDescription(0.01f));
        BodyHandle = world.Simulation.Bodies.Add(body);

        var mesh = MeshFactory.CreateShipWedge();
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
    }

    /// <summary>Convenience: get the body's current pose.</summary>
    public RigidPose Pose => _world.Simulation.Bodies[BodyHandle].Pose;

    /// <summary>Convenience: get a mutable body reference for force application.</summary>
    public BodyReference Body => _world.Simulation.Bodies[BodyHandle];

    public override void Draw(GameTime gameTime)
    {
        // Hide the ship in first-person view to avoid the model occluding the camera.
        if (_camera.Mode == Camera.CameraMode.FirstPerson) return;

        Matrix world = Pose.ToWorldMatrix();
        _renderer.DrawMesh(_vb, _ib, world, new Color(220, 220, 235));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vb.Dispose();
            _ib.Dispose();
            // Note: body removal from simulation handled by BepuWorld.Dispose via the simulation tear-down.
        }
        base.Dispose(disposing);
    }
}
