using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.GameObjects;

/// <summary>
/// A colored, axis-aligned rectangle with collision. Used for outer walls, the
/// inner divider that creates the bank/corner shot, and any in-bounds obstacles.
/// The role (wall vs obstacle) is purely a render-color distinction.
/// </summary>
public sealed class Obstacle : DrawableGameComponent
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly Color _fill;

    /// <summary>Collider registered with the physics world.</summary>
    public AabbCollider Collider { get; }

    public Obstacle(Game game, SpriteBatch spriteBatch, Texture2D pixel,
        AabbCollider collider, Color fill) : base(game)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        Collider = collider;
        _fill = fill;
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        var rect = new Rectangle(
            (int)Collider.Position.X,
            (int)Collider.Position.Y,
            (int)Collider.Size.X,
            (int)Collider.Size.Y);
        _spriteBatch.Draw(_pixel, rect, _fill);
        _spriteBatch.End();
    }
}
