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
/// A single race ring. Visually a torus; pass-through is detected via its
/// <see cref="Trigger"/>; the rim is a Bepu <see cref="BepuPhysics.Collidables.Mesh"/>
/// static so the ship physically bounces off if it hits the tube. Rendered
/// with three states:
/// <list type="bullet">
///   <item><c>IsActive</c> (next-target): bright base color + glow pass.</item>
///   <item>Unpassed and inactive: regular base color.</item>
///   <item>Passed: dimmed color, no glow.</item>
/// </list>
/// </summary>
public sealed class Ring : DrawableGameComponent
{
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly Matrix _world;
    private readonly StaticHandle _rimStatic;
    private readonly BepuWorld _bepuWorld;

    /// <summary>Center position in world space.</summary>
    public Vector3 Position { get; }

    /// <summary>Orientation; ring's tube axis = local Z rotated by this.</summary>
    public Quaternion Orientation { get; }

    /// <summary>Major radius (center of torus to center of tube).</summary>
    public float MajorRadius { get; }

    /// <summary>Minor radius (tube thickness).</summary>
    public float MinorRadius { get; }

    /// <summary>Ring's tube-axis in world space (perpendicular to the disc plane).</summary>
    public Vector3 Axis { get; }

    /// <summary>Pass-through volume.</summary>
    public Trigger Trigger { get; }

    /// <summary>True while this is the next ring the player must hit. Set by Course.</summary>
    public bool IsActive { get; set; }

    /// <summary>True after the player hits this ring (in any order).</summary>
    public bool HasBeenPassed { get; set; }

    public Ring(Game game, BepuWorld bepuWorld, PrimitiveRenderer renderer,
        Vector3 position, Quaternion orientation,
        float majorRadius = 3f, float minorRadius = 0.3f) : base(game)
    {
        _renderer = renderer;
        _bepuWorld = bepuWorld;
        Position = position;
        Orientation = orientation;
        MajorRadius = majorRadius;
        MinorRadius = minorRadius;
        DrawOrder = 10;

        Axis = Vector3.Transform(Vector3.UnitZ, orientation);
        Trigger = new Trigger(position, Axis, majorRadius - minorRadius * 0.5f);

        _world = Matrix.CreateFromQuaternion(orientation) * Matrix.CreateTranslation(position);
        var mesh = MeshFactory.CreateTorus(majorRadius, minorRadius);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);

        _rimStatic = AddRimStatic(bepuWorld, mesh, position, orientation);
    }

    /// <summary>
    /// Triangulate the torus mesh and add it as a Bepu Mesh static. The mesh's
    /// triangle buffer is allocated from <c>BepuWorld.BufferPool</c> and lives as
    /// long as the simulation; it's released when <c>BepuWorld.Dispose</c> clears
    /// the pool. Statics are infinite-mass (don't move when hit) — matches the
    /// spec for rings.
    /// </summary>
    private static StaticHandle AddRimStatic(BepuWorld world, MeshFactory.MeshData mesh,
        Vector3 position, Quaternion orientation)
    {
        int triCount = mesh.Indices.Length / 3;
        world.BufferPool.Take<Triangle>(triCount, out var triangles);
        for (int i = 0; i < triCount; i++)
        {
            var v0 = mesh.Vertices[mesh.Indices[i * 3 + 0]].Position;
            var v1 = mesh.Vertices[mesh.Indices[i * 3 + 1]].Position;
            var v2 = mesh.Vertices[mesh.Indices[i * 3 + 2]].Position;
            triangles[i] = new Triangle(
                new NumVector3(v0.X, v0.Y, v0.Z),
                new NumVector3(v1.X, v1.Y, v1.Z),
                new NumVector3(v2.X, v2.Y, v2.Z));
        }
        var bepuMesh = new BepuPhysics.Collidables.Mesh(triangles, NumVector3.One, world.BufferPool);
        var pose = new RigidPose(
            new NumVector3(position.X, position.Y, position.Z),
            new NumQuaternion(orientation.X, orientation.Y, orientation.Z, orientation.W));
        return world.Simulation.Statics.Add(new StaticDescription(
            pose,
            world.Simulation.Shapes.Add(bepuMesh)));
    }

    public override void Draw(GameTime gameTime)
    {
        Color baseColor;
        if (HasBeenPassed) baseColor = new Color(60, 60, 80);
        else if (IsActive) baseColor = new Color(220, 240, 255);
        else baseColor = new Color(140, 150, 180);

        _renderer.DrawMesh(_vb, _ib, _world, baseColor);

        if (IsActive && !HasBeenPassed)
            _renderer.DrawGlow(_vb, _ib, _world, new Color(80, 200, 255), scale: 1.08f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vb.Dispose();
            _ib.Dispose();
            // Static + mesh shape released when BepuWorld disposes the simulation + pool.
        }
        base.Dispose(disposing);
    }
}
