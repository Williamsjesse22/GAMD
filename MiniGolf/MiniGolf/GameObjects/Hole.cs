using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniGolf.GameObjects;

/// <summary>
/// The hole. Renders a dark filled circle and detects ball-drop conditions:
/// ball center within hole radius AND ball speed below
/// <see cref="MaxEntrySpeed"/>. Higher speeds let the ball skip across, which is
/// the gameplay justification for the power-meter requirement.
/// </summary>
public sealed class Hole : DrawableGameComponent
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _circle;

    /// <summary>Center of the hole in world pixels.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Visual + capture radius.</summary>
    public float Radius { get; }

    /// <summary>Ball must be moving slower than this to drop in (px/s).</summary>
    public float MaxEntrySpeed { get; set; } = 320f;

    public Hole(Game game, SpriteBatch spriteBatch, Texture2D holeCircle, Vector2 position, float radius)
        : base(game)
    {
        _spriteBatch = spriteBatch;
        _circle = holeCircle;
        Position = position;
        Radius = radius;
    }

    /// <summary>True if the ball is captured given its current position and speed.</summary>
    public bool TryCapture(Vector2 ballPosition, float ballSpeed)
    {
        Vector2 d = ballPosition - Position;
        return d.LengthSquared() <= Radius * Radius && ballSpeed <= MaxEntrySpeed;
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();
        // Hole circle texture is a filled white circle; tint it dark.
        _spriteBatch.Draw(
            _circle,
            new Vector2(Position.X - Radius, Position.Y - Radius),
            new Color(15, 15, 15));
        _spriteBatch.End();
    }
}
