using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniGolf.Physics;
using MiniGolf.Physics.Colliders;
using MiniGolf.Systems;

namespace MiniGolf.GameObjects;

/// <summary>
/// Owns the level layout for the active hole. Loads its data from a
/// <see cref="HoleLayout"/>, builds <see cref="Obstacle"/> components for each
/// wall/obstacle, holds the <see cref="SlopeRegion"/>s, and creates the
/// <see cref="Tee"/> and <see cref="Hole"/>. The host (<see cref="Game1"/>) is
/// responsible for adding/removing the per-hole components from
/// <c>Game.Components</c> when transitioning holes.
/// </summary>
public sealed class Course : DrawableGameComponent
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    /// <summary>Slope regions in this course (read by PhysicsWorld; rendered by Course).</summary>
    public List<SlopeRegion> Slopes { get; } = new();

    /// <summary>Static box colliders used by physics (walls + obstacles).</summary>
    public List<AabbCollider> StaticBoxes { get; } = new();

    /// <summary>Obstacle/wall components for rendering. Added to Game.Components by host.</summary>
    public List<Obstacle> Obstacles { get; } = new();

    /// <summary>The hole for this course.</summary>
    public Hole Hole { get; private set; } = null!;

    /// <summary>The tee for this course.</summary>
    public Tee Tee { get; private set; } = null!;

    /// <summary>Background / playing-field rectangle, fairway green.</summary>
    public Rectangle Fairway { get; private set; }

    /// <summary>Display name of the loaded hole, for HUD.</summary>
    public string LayoutName { get; private set; } = string.Empty;

    public Course(Game game, SpriteBatch spriteBatch, Texture2D pixel) : base(game)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        // Course should draw before any other DrawableGameComponent (background).
        DrawOrder = -100;
    }

    /// <summary>
    /// Load a hole from a <see cref="HoleLayout"/>. Walls render in a uniform
    /// gray; obstacles use their per-instance color so the level designer can
    /// distinguish them visually.
    /// </summary>
    /// <param name="layout">Hole data.</param>
    /// <param name="holeCircleTexture">Filled white circle texture for the hole sprite.</param>
    public void LoadLayout(HoleLayout layout, Texture2D holeCircleTexture)
    {
        Slopes.Clear();
        StaticBoxes.Clear();
        Obstacles.Clear();

        Fairway = layout.Fairway;
        LayoutName = layout.Name;

        Color wallColor = new(80, 80, 90);
        for (int i = 0; i < layout.Walls.Count; i++) AddBox(layout.Walls[i], wallColor);
        for (int i = 0; i < layout.Obstacles.Count; i++) AddBox(layout.Obstacles[i].Box, layout.Obstacles[i].Color);

        Slopes.AddRange(layout.Slopes);

        Tee = new Tee(Game, _spriteBatch, _pixel, layout.TeeRect);
        Hole = new Hole(Game, _spriteBatch, holeCircleTexture, layout.HolePosition, layout.HoleRadius)
        {
            MaxEntrySpeed = layout.HoleMaxEntrySpeed,
        };
    }

    private void AddBox(AabbCollider box, Color color)
    {
        StaticBoxes.Add(box);
        Obstacles.Add(new Obstacle(Game, _spriteBatch, _pixel, box, color));
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        _spriteBatch.Draw(_pixel, Fairway, new Color(40, 130, 60));

        for (int i = 0; i < Slopes.Count; i++)
        {
            var s = Slopes[i];
            var rect = new Rectangle((int)s.Position.X, (int)s.Position.Y,
                                      (int)s.Size.X, (int)s.Size.Y);
            _spriteBatch.Draw(_pixel, rect, s.Tint);

            if (s.Cells is null) DrawUniformArrows(s);
            else                 DrawGridArrows(s);

            if (!string.IsNullOrEmpty(s.Label)) DrawSlopeLabel(s, s.Label!);
        }

        _spriteBatch.End();
    }

    private void DrawSlopeLabel(SlopeRegion s, string label)
    {
        const int scale = 3;
        const int glyphW = 5, glyphH = 7, kern = 1;
        int textWidth = (label.Length * (glyphW + kern) - kern) * scale;
        int textHeight = glyphH * scale;
        Vector2 center = s.Position + s.Size * 0.5f;
        Vector2 pos = new(center.X - textWidth / 2f, center.Y - textHeight / 2f);
        BitmapText.Draw(_spriteBatch, _pixel, label, pos, Color.White, scale);
    }

    private void DrawUniformArrows(SlopeRegion s)
    {
        Vector2 dir = s.Acceleration;
        if (dir.LengthSquared() < 1e-3f) return;
        float magnitude = dir.Length();
        dir /= magnitude;

        // 5x3 grid of decorative chevrons; length scaled by the field's magnitude.
        int cols = 5, rows = 3;
        Vector2 cell = new(s.Size.X / cols, s.Size.Y / rows);
        float length = MathHelper.Clamp(magnitude * 0.06f, 8f, 22f);
        for (int gy = 0; gy < rows; gy++)
        {
            for (int gx = 0; gx < cols; gx++)
            {
                Vector2 c = s.Position + new Vector2((gx + 0.5f) * cell.X, (gy + 0.5f) * cell.Y);
                DrawArrow(c, dir, length, Color.White * 0.45f);
            }
        }
    }

    private void DrawGridArrows(SlopeRegion s)
    {
        if (s.Cells is null) return;
        int rows = s.Cells.GetLength(0);
        int cols = s.Cells.GetLength(1);
        if (rows == 0 || cols == 0) return;

        Vector2 cell = new(s.Size.X / cols, s.Size.Y / rows);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 v = s.Cells[r, c];
                float magnitude = v.Length();
                if (magnitude < 1e-3f) continue;
                Vector2 dir = v / magnitude;

                Vector2 center = s.Position + new Vector2((c + 0.5f) * cell.X, (r + 0.5f) * cell.Y);
                // Length proportional to magnitude, capped to ~80% of cell's smaller dimension.
                float maxLen = MathF.Min(cell.X, cell.Y) * 0.8f;
                float length = MathHelper.Clamp(magnitude * 0.06f, 6f, maxLen);
                DrawArrow(center, dir, length, Color.White * 0.7f);
            }
        }
    }

    private void DrawArrow(Vector2 center, Vector2 dir, float length, Color color)
    {
        Vector2 tail = center - dir * (length * 0.5f);
        Vector2 head = center + dir * (length * 0.5f);
        DrawLineThin(tail, head, color);

        // Head fins at +/- 135°.
        const float a = 2.356f; // 135° in radians
        Vector2 finA = head + Rotate(-dir, a) * (length * 0.4f);
        Vector2 finB = head + Rotate(-dir, -a) * (length * 0.4f);
        DrawLineThin(head, finA, color);
        DrawLineThin(head, finB, color);
    }

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    private void DrawLineThin(Vector2 a, Vector2 b, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 1f) return;
        d /= len;
        for (float t = 0; t < len; t += 1f)
        {
            Vector2 p = a + d * t;
            _spriteBatch.Draw(_pixel, new Rectangle((int)p.X, (int)p.Y, 1, 1), color);
        }
    }
}
