using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceRace.Graphics;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.GameObjects;

/// <summary>
/// A stationary, no-body gravity source. Each tick applies an inverse-square
/// pull force to the ship within <see cref="MaxRange"/>; renders as a
/// purple-glowing sphere so the player can see and avoid it.
/// </summary>
public sealed class GravityWell : DrawableGameComponent
{
    private readonly Ship _ship;
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly Matrix _world;

    public NumVector3 Position { get; }

    /// <summary>Force coefficient. Effective force at distance d ≈ Strength / d².</summary>
    public float Strength { get; set; }

    /// <summary>Wells beyond this distance contribute nothing (cheap-out).</summary>
    public float MaxRange { get; set; } = 60f;

    /// <summary>Visual sphere radius (rendering only — does not affect the force).</summary>
    public float VisualRadius { get; }

    public GravityWell(Game game, Ship ship, PrimitiveRenderer renderer,
        NumVector3 position, float strength = 250f, float visualRadius = 1.5f) : base(game)
    {
        _ship = ship;
        _renderer = renderer;
        Position = position;
        Strength = strength;
        VisualRadius = visualRadius;
        DrawOrder = 7;

        _world = Matrix.CreateTranslation(position.X, position.Y, position.Z);
        var mesh = MeshFactory.CreateSphere(visualRadius, 12, 18);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var bodyRef = _ship.Body;
        NumVector3 toShip = bodyRef.Pose.Position - Position;
        float dist = toShip.Length();
        if (dist > MaxRange || dist < 1e-3f) return;
        NumVector3 direction = toShip / dist;
        float forceMagnitude = Strength / (dist * dist);
        // Pull TOWARD well: impulse = -direction (which points away from well).
        NumVector3 impulse = -direction * forceMagnitude * _ship.Mass * dt;
        bodyRef.ApplyImpulse(impulse, NumVector3.Zero);
        bodyRef.Awake = true;
    }

    public override void Draw(GameTime gameTime)
    {
        _renderer.DrawMesh(_vb, _ib, _world, new Color(110, 60, 200));
        _renderer.DrawGlow(_vb, _ib, _world, new Color(170, 80, 240), scale: 1.4f);
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
