using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniGolf.Physics;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.GameObjects;

/// <summary>
/// The golf ball. Wraps a <see cref="RigidBody"/> + <see cref="CircleCollider"/>
/// and renders a small filled circle. Position lives on the body, not the component.
/// </summary>
public sealed class Ball : DrawableGameComponent
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _circle;

    /// <summary>Underlying physics body.</summary>
    public RigidBody Body { get; } = new()
    {
        Mass = 1f,
        LinearDamping = 1.05f,
        Restitution = 0.78f,
    };

    /// <summary>Collider associated with this ball — registered with the physics world.</summary>
    public CircleCollider Collider { get; }

    /// <summary>Ball radius (also used by visual rendering).</summary>
    public float Radius { get; }

    public Ball(Game game, SpriteBatch spriteBatch, Texture2D circleTexture, float radius = 9f)
        : base(game)
    {
        _spriteBatch = spriteBatch;
        _circle = circleTexture;
        Radius = radius;
        Collider = new CircleCollider(Body, radius);
    }

    /// <summary>Place the ball at a fixed position with zero velocity.</summary>
    public void Reset(Vector2 position)
    {
        Body.Position = position;
        Body.Velocity = Vector2.Zero;
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        var p = Body.Position;
        // _circle is a filled white circle of size (Radius*2)x(Radius*2).
        _spriteBatch.Draw(
            _circle,
            new Vector2(p.X - Radius, p.Y - Radius),
            Color.White);
        _spriteBatch.End();
    }
}
