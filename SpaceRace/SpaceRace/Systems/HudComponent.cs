using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceRace.Systems;

/// <summary>
/// Heads-up display for the race: clock + missed count + score during the race,
/// PreRace countdown overlay, and Finished overlay with final time + score.
/// Uses the procedural <see cref="BitmapText"/> font so no `.spritefont` content
/// is required; <see cref="TextureFactory.CreatePixel"/> supplies the
/// 1×1 quad used for translucent panels and bars.
/// </summary>
public sealed class HudComponent : DrawableGameComponent
{
    /// <summary>Mirrors <c>Game1.GameState</c> so the HUD knows what to render.</summary>
    public enum RaceState { PreRace, Racing, Finished }

    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    /// <summary>Race state (driven by Game1).</summary>
    public RaceState State { get; set; } = RaceState.PreRace;

    /// <summary>Elapsed race time in seconds. Game1 counts up while in Racing.</summary>
    public float ElapsedSeconds { get; set; }

    /// <summary>Seconds remaining in the PreRace countdown (3 → 0).</summary>
    public float CountdownSeconds { get; set; } = 3f;

    /// <summary>Number of rings missed (Course.MissedCount).</summary>
    public int MissedCount { get; set; }

    /// <summary>Index of the next target ring (Course.CurrentTargetIndex).</summary>
    public int CurrentTargetIndex { get; set; }

    /// <summary>Total ring count for the active course.</summary>
    public int RingCount { get; set; }

    /// <summary>Final score; computed by Game1 each Update via <see cref="ComputeScore"/>.</summary>
    public int Score { get; set; }

    /// <summary>Current fuel level 0..1. Only rendered when <see cref="ShowFuel"/> is true.</summary>
    public float Fuel { get; set; } = 1f;

    /// <summary>Whether to render the fuel bar (set true when fuel mode is active).</summary>
    public bool ShowFuel { get; set; }

    public HudComponent(Game game, SpriteBatch spriteBatch, Texture2D pixel) : base(game)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        DrawOrder = 1000;
    }

    /// <summary>
    /// Score = max(0, max(0, 1000 - time) - 30·missed). Skipping a ring (~30 pts)
    /// always costs more than the time saved. Clamped to 0 to keep the HUD clean.
    /// </summary>
    public static int ComputeScore(float timeSeconds, int missedCount)
    {
        int timePart = Math.Max(0, 1000 - (int)timeSeconds);
        int penalty = 30 * missedCount;
        return Math.Max(0, timePart - penalty);
    }

    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        DrawScoreboard();

        if (State == RaceState.PreRace) DrawCountdownOverlay();
        else if (State == RaceState.Finished) DrawFinishedOverlay();

        _spriteBatch.End();
    }

    private void DrawScoreboard()
    {
        // Top-left stack: TIME / RING X/Y / MISSED / SCORE
        DrawText($"TIME {FormatTime(ElapsedSeconds)}", new Vector2(20, 20), Color.White, scale: 3);
        if (RingCount > 0)
            DrawText($"RING {Math.Min(CurrentTargetIndex + 1, RingCount)}/{RingCount}",
                     new Vector2(20, 60), new Color(220, 220, 240), scale: 2);
        DrawText($"MISSED {MissedCount}", new Vector2(20, 90), new Color(240, 200, 120), scale: 2);
        DrawText($"SCORE {Score}", new Vector2(20, 120), Color.White, scale: 2);

        if (ShowFuel) DrawFuelBar();
    }

    private void DrawFuelBar()
    {
        const int barX = 20, barW = 220, barH = 14;
        int barY = 670;
        // Background.
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barW, barH), new Color(40, 40, 40));
        // Fill: green→yellow→red as fuel drops.
        float t = MathHelper.Clamp(Fuel, 0f, 1f);
        Color fill = new(
            (byte)((1f - t) * 240),
            (byte)(t * 220),
            40);
        int fillW = (int)(barW * t);
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, fillW, barH), fill);
        // Outline.
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barW, 1), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY + barH - 1, barW, 1), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(barX, barY, 1, barH), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(barX + barW - 1, barY, 1, barH), Color.White);
        // Label.
        DrawText("FUEL", new Vector2(barX, barY - 22), Color.White, scale: 2);
    }

    private void DrawCountdownOverlay()
    {
        DrawCenteredPanel();
        // Show 3 / 2 / 1 / GO! based on countdown bucket.
        string text;
        int bucket = (int)Math.Ceiling(CountdownSeconds);
        if (bucket >= 3) text = "3";
        else if (bucket == 2) text = "2";
        else if (bucket == 1) text = "1";
        else text = "GO!";
        DrawCentered(text, new Vector2(640, 360), Color.White, scale: 10);
    }

    private void DrawFinishedOverlay()
    {
        DrawCenteredPanel();
        DrawCentered("RACE COMPLETE!", new Vector2(640, 290), Color.White, scale: 6);
        DrawCentered($"TIME {FormatTime(ElapsedSeconds)}", new Vector2(640, 370), Color.White, scale: 5);
        DrawCentered($"MISSED {MissedCount}", new Vector2(640, 420), new Color(240, 200, 120), scale: 4);
        DrawCentered($"SCORE {Score}", new Vector2(640, 470), Color.White, scale: 5);
        DrawCentered("PRESS R TO RESTART", new Vector2(640, 540), new Color(180, 180, 200), scale: 2);
    }

    private void DrawCenteredPanel()
    {
        var panel = new Rectangle(0, 240, 1280, 320);
        _spriteBatch.Draw(_pixel, panel, new Color(0, 0, 0, 180));
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
        BitmapText.Draw(_spriteBatch, _pixel, text, pos, color, scale);
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = (int)Math.Max(0, seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes}:{secs:00}";
    }
}
