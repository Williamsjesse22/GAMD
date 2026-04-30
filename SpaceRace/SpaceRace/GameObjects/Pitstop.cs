using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceRace.Graphics;
using SpaceRace.Physics;

namespace SpaceRace.GameObjects;

/// <summary>
/// A small green ring the ship can fly through to refuel. Reuses the generic
/// <see cref="Trigger"/> pass-through volume; visually a smaller torus than
/// race rings, with a green glow accent so it's distinct.
/// </summary>
public sealed class Pitstop : DrawableGameComponent
{
    private readonly Ship _ship;
    private readonly PrimitiveRenderer _renderer;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly Matrix _world;

    /// <summary>Pass-through volume.</summary>
    public Trigger Trigger { get; }

    /// <summary>Fuel restored per pass (0..1).</summary>
    public float RefuelAmount { get; set; } = 0.6f;

    /// <summary>Fired after a successful pass-through.</summary>
    public event Action<Ship>? OnPassed;

    private Vector3 _previousShipPosition;
    private bool _hasPreviousPosition;

    public Pitstop(Game game, Ship ship, PrimitiveRenderer renderer,
        Vector3 position, Quaternion orientation,
        float majorRadius = 2f, float minorRadius = 0.2f) : base(game)
    {
        _ship = ship;
        _renderer = renderer;
        DrawOrder = 8;

        Vector3 axis = Vector3.Transform(Vector3.UnitZ, orientation);
        Trigger = new Trigger(position, axis, majorRadius - minorRadius * 0.5f);

        _world = Matrix.CreateFromQuaternion(orientation) * Matrix.CreateTranslation(position);
        var mesh = MeshFactory.CreateTorus(majorRadius, minorRadius);
        (_vb, _ib) = mesh.ToGpu(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 shipPos = ToXna(_ship.Pose.Position);
        if (!_hasPreviousPosition)
        {
            _previousShipPosition = shipPos;
            _hasPreviousPosition = true;
            return;
        }
        if (Trigger.TryPassThrough(_previousShipPosition, shipPos, out _))
        {
            _ship.Fuel = MathHelper.Clamp(_ship.Fuel + RefuelAmount, 0f, 1f);
            OnPassed?.Invoke(_ship);
        }
        _previousShipPosition = shipPos;
    }

    public override void Draw(GameTime gameTime)
    {
        _renderer.DrawMesh(_vb, _ib, _world, new Color(140, 230, 140));
        _renderer.DrawGlow(_vb, _ib, _world, new Color(80, 220, 100), scale: 1.06f);
    }

    private static Vector3 ToXna(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

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
