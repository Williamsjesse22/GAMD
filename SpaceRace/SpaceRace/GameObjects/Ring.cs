using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceRace.Graphics;
using SpaceRace.Physics;

namespace SpaceRace.GameObjects;

/// <summary>
/// A single race ring. Visually a torus; pass-through is detected via its
/// <see cref="Trigger"/>. Rendered with three states:
/// <list type="bullet">
///   <item><c>IsActive</c> (next-target): bright base color + glow pass.</item>
///   <item>Unpassed and inactive: regular base color.</item>
///   <item>Passed: dimmed color, no glow.</item>
/// </list>
/// v1 has no Bepu collision body — the ship can fly through the torus's solid
/// rim freely. A future revision can add a <c>Bepu.Mesh</c> static for rim collision.
/// </summary>
public sealed class Ring : DrawableGameComponent
{
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly Matrix _world;

    /// <summary>Center position in world space.</summary>
    public Vector3 Position { get; }

    /// <summary>Orientation; ring's tube axis = local Y rotated by this.</summary>
    public Quaternion Orientation { get; }

    /// <summary>Major radius (center of torus to center of tube).</summary>
    public float MajorRadius { get; }

    /// <summary>Minor radius (tube thickness).</summary>
    public float MinorRadius { get; }

    /// <summary>Pass-through volume.</summary>
    public Trigger Trigger { get; }

    /// <summary>True while this is the next ring the player must hit. Set by Course.</summary>
    public bool IsActive { get; set; }

    /// <summary>True after the player hits this ring (in any order).</summary>
    public bool HasBeenPassed { get; set; }

    public Ring(Game game, PrimitiveRenderer renderer, Vector3 position, Quaternion orientation,
        float majorRadius = 3f, float minorRadius = 0.3f) : base(game)
    {
        _renderer = renderer;
        Position = position;
        Orientation = orientation;
        MajorRadius = majorRadius;
        MinorRadius = minorRadius;
        DrawOrder = 10;

        // Torus mesh is built around Z, so the hole opens along ±Z; rotate by orientation.
        Vector3 axis = Vector3.Transform(Vector3.UnitZ, orientation);
        // Inner radius accounts for the tube; ship must be inside (major - minor) to pass cleanly.
        Trigger = new Trigger(position, axis, majorRadius - minorRadius * 0.5f);

        _world = Matrix.CreateFromQuaternion(orientation) * Matrix.CreateTranslation(position);
        var mesh = MeshFactory.CreateTorus(majorRadius, minorRadius);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
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
        }
        base.Dispose(disposing);
    }
}
