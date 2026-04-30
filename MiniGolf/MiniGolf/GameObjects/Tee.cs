using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniGolf.GameObjects;

/// <summary>
/// The blue rectangle the ball launches from. Visual only — no collision.
/// Required by the assignment ("ball launched from a blue rectangle").
/// </summary>
public sealed class Tee : DrawableGameComponent
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    /// <summary>Tee rectangle in world pixels.</summary>
    public Rectangle Rectangle { get; set; }

    /// <summary>Center of the tee — ball respawn position.</summary>
    public Vector2 Center => new(Rectangle.X + Rectangle.Width / 2f,
                                  Rectangle.Y + Rectangle.Height / 2f);

    public Tee(Game game, SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect) : base(game)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        Rectangle = rect;
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        _spriteBatch.Draw(_pixel, Rectangle, new Color(60, 110, 220));
        // White outline for definition.
        _spriteBatch.Draw(_pixel, new Rectangle(Rectangle.X, Rectangle.Y, Rectangle.Width, 1), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(Rectangle.X, Rectangle.Bottom - 1, Rectangle.Width, 1), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(Rectangle.X, Rectangle.Y, 1, Rectangle.Height), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(Rectangle.Right - 1, Rectangle.Y, 1, Rectangle.Height), Color.White);
        _spriteBatch.End();
    }
}
