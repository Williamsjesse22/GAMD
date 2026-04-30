using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniGolf.Systems;

/// <summary>
/// Heads-up display: aim line (when the ball is at rest), power meter (while
/// charging), per-hole + total stroke counts, hole index, and end-of-hole /
/// end-of-game overlay messages. Procedural-only — uses a 1x1 white pixel and
/// the built-in <see cref="BitmapText"/> fallback so the HUD renders without
/// any content-pipeline assets.
/// </summary>
public sealed class HudComponent : DrawableGameComponent
{
    private readonly InputManager _input;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private SpriteFont? _font;

    /// <summary>Stroke count for the current hole. Game1 increments on each shot.</summary>
    public int StrokeCount { get; set; }

    /// <summary>Cumulative stroke count across all holes in this session.</summary>
    public int TotalStrokes { get; set; }

    /// <summary>Zero-based index of the current hole.</summary>
    public int HoleIndex { get; set; }

    /// <summary>Number of holes in the current session.</summary>
    public int HoleCount { get; set; }

    /// <summary>Display name of the current hole layout (e.g. "CLASSIC").</summary>
    public string HoleName { get; set; } = string.Empty;

    /// <summary>True while the post-sink hold is active. Triggers center overlay.</summary>
    public bool HoleComplete { get; set; }

    /// <summary>True after the last hole has been sunk. Triggers end-of-game overlay.</summary>
    public bool GameComplete { get; set; }

    /// <summary>Lower-left anchor for the power meter.</summary>
    public Vector2 MeterAnchor { get; set; } = new Vector2(20, 670);

    public HudComponent(Game game, SpriteBatch spriteBatch, Texture2D pixel, InputManager input)
        : base(game)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _input = input;
    }

    /// <summary>Optional: assign a SpriteFont (loaded by the host) for crisp text.</summary>
    public void SetFont(SpriteFont? font) => _font = font;

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        DrawAimLine();
        DrawPowerMeter();
        DrawScoreboard();
        DrawHint();
        DrawCenterOverlay();

        _spriteBatch.End();
    }

    private void DrawScoreboard()
    {
        // Three lines stacked top-left.
        Vector2 holeLine = new(20, 20);
        Vector2 strokeLine = new(20, 60);
        Vector2 totalLine = new(20, 100);

        if (HoleCount > 0)
            DrawText($"HOLE {HoleIndex + 1}/{HoleCount}", holeLine, Color.White, scale: 3);
        DrawText($"STROKES: {StrokeCount}", strokeLine, Color.White, scale: 3);
        DrawText($"TOTAL: {TotalStrokes}", totalLine, new Color(220, 220, 220), scale: 2);
    }

    private void DrawHint()
    {
        if (HoleComplete || GameComplete) return;

        string hint = _input.State switch
        {
            InputManager.InputState.Aiming => "HOLD TO CHARGE",
            InputManager.InputState.Charging => "LET GO TO FIRE",
            _ => string.Empty,
        };
        if (hint.Length == 0) return;

        Vector2 pos = new(MeterAnchor.X, MeterAnchor.Y - 26);
        DrawText(hint, pos, Color.White, scale: 2);
    }

    private void DrawAimLine()
    {
        if (!_input.BallAtRest) return;
        if (_input.State != InputManager.InputState.Aiming &&
            _input.State != InputManager.InputState.Charging) return;

        Vector2 start = _input.BallPosition;
        Vector2 dir = _input.AimDirection;
        const int dots = 18;
        const float spacing = 12f;
        for (int i = 1; i <= dots; i++)
        {
            Vector2 p = start + dir * (i * spacing);
            DrawFilledRect(new Rectangle((int)p.X - 2, (int)p.Y - 2, 4, 4), Color.White * 0.65f);
        }
    }

    private void DrawPowerMeter()
    {
        if (HoleComplete || GameComplete) return;

        const int barWidth = 220;
        const int barHeight = 18;
        const int notchHeight = 22;

        Vector2 anchor = MeterAnchor;
        DrawFilledRect(new Rectangle((int)anchor.X, (int)anchor.Y, barWidth, barHeight),
            new Color(40, 40, 40));

        for (int i = 1; i < InputManager.PowerLevels; i++)
        {
            int x = (int)anchor.X + (barWidth * i) / InputManager.PowerLevels;
            DrawFilledRect(new Rectangle(x, (int)anchor.Y, 1, barHeight), new Color(80, 80, 80));
        }

        if (_input.State == InputManager.InputState.Charging)
        {
            int filled = (int)(barWidth * _input.ChargeFraction01);
            float t = _input.ChargeFraction01;
            Color c = new(
                (byte)MathHelper.Clamp(t * 2f * 255, 0, 255),
                (byte)MathHelper.Clamp((1f - System.MathF.Abs(t - 0.5f) * 2f) * 255, 0, 255),
                0);
            DrawFilledRect(new Rectangle((int)anchor.X, (int)anchor.Y, filled, barHeight), c);

            int notchX = (int)anchor.X + (barWidth * _input.PowerLevel) / InputManager.PowerLevels - 1;
            DrawFilledRect(new Rectangle(notchX, (int)anchor.Y - 2, 2, notchHeight), Color.White);
        }

        DrawRectOutline(new Rectangle((int)anchor.X, (int)anchor.Y, barWidth, barHeight), Color.White);
    }

    private void DrawCenterOverlay()
    {
        if (!HoleComplete && !GameComplete) return;

        // Translucent black panel across the screen center.
        var panel = new Rectangle(0, 270, 1280, 180);
        DrawFilledRect(panel, new Color(0, 0, 0, 170));

        if (GameComplete)
        {
            DrawCentered("GAME COMPLETE!", new Vector2(640, 320), Color.White, scale: 6);
            DrawCentered($"TOTAL STROKES: {TotalStrokes}", new Vector2(640, 400), Color.White, scale: 4);
        }
        else
        {
            DrawCentered("HOLE COMPLETE!", new Vector2(640, 340), Color.White, scale: 6);
        }
    }

    private void DrawCentered(string text, Vector2 center, Color color, int scale)
    {
        const int glyphW = 5, glyphH = 7, kern = 1;
        int w = (text.Length * (glyphW + kern) - kern) * scale;
        int h = glyphH * scale;
        Vector2 pos = new(center.X - w / 2f, center.Y - h / 2f);
        DrawText(text, pos, color, scale);
    }

    private void DrawText(string text, Vector2 pos, Color color, int scale)
    {
        if (_font != null)
        {
            _spriteBatch.DrawString(_font, text, pos, color);
            return;
        }
        BitmapText.Draw(_spriteBatch, _pixel, text, pos, color, scale);
    }

    private void DrawFilledRect(Rectangle r, Color c)
    {
        _spriteBatch.Draw(_pixel, r, c);
    }

    private void DrawRectOutline(Rectangle r, Color c)
    {
        DrawFilledRect(new Rectangle(r.X, r.Y, r.Width, 1), c);
        DrawFilledRect(new Rectangle(r.X, r.Bottom - 1, r.Width, 1), c);
        DrawFilledRect(new Rectangle(r.X, r.Y, 1, r.Height), c);
        DrawFilledRect(new Rectangle(r.Right - 1, r.Y, 1, r.Height), c);
    }
}
