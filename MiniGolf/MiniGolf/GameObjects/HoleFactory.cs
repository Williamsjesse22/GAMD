using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MiniGolf.Physics;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.GameObjects;

/// <summary>
/// Builders for hole layouts. <see cref="Hole1Classic"/> and <see cref="Hole2Funnel"/>
/// are hand-crafted; <see cref="Hole3Procedural"/> generates a fresh layout from a
/// parametric L-shape template each call. <see cref="BuildAll"/> returns the
/// canonical 3-hole sequence for a play session.
/// </summary>
public static class HoleFactory
{
    private const int FieldX = 50;
    private const int FieldY = 50;
    private const int FieldW = 1180;
    private const int FieldH = 620;
    private const int WallThickness = 16;

    /// <summary>The 4-hole sequence used by Game1: Classic → Funnel → Procedural → Chaos.</summary>
    public static List<HoleLayout> BuildAll(Random rng) => new()
    {
        Hole1Classic(),
        Hole2Funnel(),
        Hole3Procedural(rng),
        Hole4Chaos(rng),
    };

    /// <summary>Hole 1: L-shape with a divider creating a bank shot, one uphill + one downhill.</summary>
    public static HoleLayout Hole1Classic()
    {
        var layout = new HoleLayout
        {
            Name = "CLASSIC",
            Fairway = new Rectangle(FieldX, FieldY, FieldW, FieldH),
            TeeRect = new Rectangle(180, 580, 40, 30),
            HolePosition = new Vector2(1100, 590),
        };
        AddOuterWalls(layout);

        // Inner divider — forces a bank/corner shot.
        layout.Walls.Add(new AabbCollider(632, 350, WallThickness, 304));

        // Square obstacle in the upper region.
        layout.Obstacles.Add((new AabbCollider(400, 220, 80, 80), new Color(170, 110, 60)));

        // Uphill (red) in the lower-left chamber, decelerates northward motion.
        layout.Slopes.Add(new SlopeRegion(
            position: new Vector2(200, 380),
            size: new Vector2(360, 150),
            acceleration: new Vector2(0f, 280f),
            tint: new Color(220, 60, 60, 180),
            label: "UPHILL"));

        // Downhill (green) leading into the hole.
        layout.Slopes.Add(new SlopeRegion(
            position: new Vector2(820, 540),
            size: new Vector2(280, 100),
            acceleration: new Vector2(220f, 0f),
            tint: new Color(60, 200, 90, 170),
            label: "DOWNHILL"));

        return layout;
    }

    /// <summary>
    /// Hole 2: open chamber with a single big slope grid whose cells all point at
    /// the hole, creating a funnel/bowl effect. Demonstrates the slope-grid feature.
    /// </summary>
    public static HoleLayout Hole2Funnel()
    {
        var layout = new HoleLayout
        {
            Name = "FUNNEL",
            Fairway = new Rectangle(FieldX, FieldY, FieldW, FieldH),
            TeeRect = new Rectangle(180, 580, 40, 30),
            HolePosition = new Vector2(900, 200),
        };
        AddOuterWalls(layout);

        // Two pillar obstacles to add navigational interest.
        layout.Obstacles.Add((new AabbCollider(560, 280, 60, 60), new Color(170, 110, 60)));
        layout.Obstacles.Add((new AabbCollider(360, 460, 60, 60), new Color(170, 110, 60)));

        // Big slope grid covering the middle of the field. Each cell points at the
        // hole; magnitude grows with distance so far-away balls get a strong nudge
        // and balls near the hole roll in gently.
        Vector2 slopePos = new(300, 120);
        Vector2 slopeSize = new(680, 500);
        const int rows = 5, cols = 8;
        var cells = new Vector2[rows, cols];
        Vector2 cellSize = new(slopeSize.X / cols, slopeSize.Y / rows);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 cellCenter = slopePos + new Vector2((c + 0.5f) * cellSize.X, (r + 0.5f) * cellSize.Y);
                Vector2 toHole = layout.HolePosition - cellCenter;
                float dist = toHole.Length();
                if (dist < 1f) { cells[r, c] = Vector2.Zero; continue; }
                Vector2 dir = toHole / dist;
                // Magnitude scales with distance, capped so ball near the hole
                // doesn't get blasted past the cup.
                float magnitude = MathHelper.Clamp(dist * 0.6f, 80f, 260f);
                cells[r, c] = dir * magnitude;
            }
        }
        layout.Slopes.Add(new SlopeRegion(
            position: slopePos,
            size: slopeSize,
            acceleration: Vector2.Zero,
            tint: new Color(60, 180, 200, 90))
        {
            Cells = cells,
        });

        return layout;
    }

    /// <summary>
    /// Hole 3: parametric L-shape with randomized divider, slope, obstacle, and
    /// hole positions. Reroll-on-conflict ensures the obstacle never spawns
    /// inside the tee or on top of the hole.
    /// </summary>
    public static HoleLayout Hole3Procedural(Random rng)
    {
        var layout = new HoleLayout
        {
            Name = "PROCEDURAL",
            Fairway = new Rectangle(FieldX, FieldY, FieldW, FieldH),
        };
        AddOuterWalls(layout);

        // Tee on the left, hole on the right; vary Y so each playthrough differs.
        int teeY = rng.Next(150, 600);
        layout.TeeRect = new Rectangle(120, teeY, 40, 30);

        int holeY;
        do { holeY = rng.Next(120, 600); }
        while (Math.Abs(holeY - teeY) < 80);
        layout.HolePosition = new Vector2(1080, holeY);

        // Inner divider — vertical wall sticking from top OR bottom, randomized.
        int dividerX = rng.Next(500, 800);
        bool fromTop = rng.Next(2) == 0;
        if (fromTop)
        {
            int height = rng.Next(220, 380);
            layout.Walls.Add(new AabbCollider(dividerX, FieldY, WallThickness, height));
        }
        else
        {
            int height = rng.Next(220, 380);
            layout.Walls.Add(new AabbCollider(dividerX, FieldY + FieldH - height, WallThickness, height));
        }

        // Random obstacle, retried until it clears tee + hole.
        Rectangle teeBuffer = Inflate(layout.TeeRect, 24);
        AabbCollider? obs = TryPlaceBox(rng, 60, 60, teeBuffer, layout.HolePosition, 32);
        if (obs is not null)
            layout.Obstacles.Add((obs, new Color(170, 110, 60)));

        // Random slope: 50/50 uphill/downhill, axis-aligned, placed in the
        // middle band so it sits in the path from tee to hole.
        bool uphill = rng.Next(2) == 0;
        int slopeX = rng.Next(300, 760);
        int slopeY = rng.Next(160, 480);
        var slopeAccel = uphill ? new Vector2(0f, 260f) : new Vector2(0f, -240f);
        layout.Slopes.Add(new SlopeRegion(
            position: new Vector2(slopeX, slopeY),
            size: new Vector2(220, 120),
            acceleration: slopeAccel,
            tint: uphill ? new Color(220, 60, 60, 180) : new Color(60, 200, 90, 170),
            label: uphill ? "UPHILL" : "DOWNHILL"));

        return layout;
    }

    /// <summary>
    /// Hole 4: maximum chaos. Tee and hole are placed in random diagonally
    /// opposite corners of the field; 2-4 inner walls of mixed orientation, 2-4
    /// random-color obstacles of varied sizes, and 1-3 slope regions chosen
    /// from four field shapes (uniform, funnel toward a pivot, swirl/vortex
    /// around a center, side-gradient with magnitude that varies across cells).
    /// </summary>
    public static HoleLayout Hole4Chaos(Random rng)
    {
        var layout = new HoleLayout
        {
            Name = "CHAOS",
            Fairway = new Rectangle(FieldX, FieldY, FieldW, FieldH),
        };
        AddOuterWalls(layout);

        var (tee, hole) = PickDiagonalTeeHole(rng);
        layout.TeeRect = tee;
        layout.HolePosition = hole;

        Rectangle teeBuffer = Inflate(tee, 32);
        const float HoleBuffer = 60f;

        // 2-4 inner walls, mixed horizontal/vertical, varied lengths.
        int wallCount = rng.Next(2, 5);
        for (int i = 0; i < wallCount; i++)
        {
            var wall = TryPlaceWall(rng, teeBuffer, hole, HoleBuffer);
            if (wall is not null) layout.Walls.Add(wall);
        }

        // 2-4 obstacles with random sizes and earth-tone colors.
        int obsCount = rng.Next(2, 5);
        for (int i = 0; i < obsCount; i++)
        {
            int w = rng.Next(40, 100);
            int h = rng.Next(40, 100);
            var box = TryPlaceBox(rng, w, h, teeBuffer, hole, (int)HoleBuffer);
            if (box is not null) layout.Obstacles.Add((box, RandomObstacleColor(rng)));
        }

        // 1-3 slope regions, each from one of four field types.
        int slopeCount = rng.Next(1, 4);
        for (int i = 0; i < slopeCount; i++)
        {
            var slope = MakeRandomSlope(rng, hole);
            if (slope is not null) layout.Slopes.Add(slope);
        }

        return layout;
    }

    private static (Rectangle Tee, Vector2 Hole) PickDiagonalTeeHole(Random rng)
    {
        // 4 corners: 0=BL, 1=TL, 2=TR, 3=BR. Hole goes diagonally opposite.
        int teeCorner = rng.Next(4);
        int holeCorner = (teeCorner + 2) % 4;
        Rectangle tee = CornerTee(rng, teeCorner);
        Vector2 hole = CornerHole(rng, holeCorner);
        return (tee, hole);
    }

    private static Rectangle CornerTee(Random rng, int corner) => corner switch
    {
        0 => new Rectangle(rng.Next(120, 240), rng.Next(540, 620), 40, 30), // bottom-left
        1 => new Rectangle(rng.Next(120, 240), rng.Next(110, 180), 40, 30), // top-left
        2 => new Rectangle(rng.Next(1000, 1110), rng.Next(110, 180), 40, 30), // top-right
        _ => new Rectangle(rng.Next(1000, 1110), rng.Next(540, 620), 40, 30), // bottom-right
    };

    private static Vector2 CornerHole(Random rng, int corner) => corner switch
    {
        0 => new Vector2(rng.Next(140, 260), rng.Next(530, 620)),
        1 => new Vector2(rng.Next(140, 260), rng.Next(120, 200)),
        2 => new Vector2(rng.Next(1020, 1140), rng.Next(120, 200)),
        _ => new Vector2(rng.Next(1020, 1140), rng.Next(530, 620)),
    };

    private static AabbCollider? TryPlaceWall(Random rng, Rectangle teeBuffer, Vector2 holePos, float holeBuffer)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            bool vertical = rng.Next(2) == 0;
            int length = rng.Next(140, 320);
            int x, y, w, h;
            if (vertical)
            {
                w = WallThickness; h = length;
                x = rng.Next(FieldX + 80, FieldX + FieldW - 80 - w);
                y = rng.Next(FieldY + 30, FieldY + FieldH - 30 - h);
            }
            else
            {
                w = length; h = WallThickness;
                x = rng.Next(FieldX + 30, FieldX + FieldW - 30 - w);
                y = rng.Next(FieldY + 80, FieldY + FieldH - 80 - h);
            }
            var box = new Rectangle(x, y, w, h);
            if (box.Intersects(teeBuffer)) continue;
            if (DistanceFromPoint(box, holePos) < holeBuffer) continue;
            return new AabbCollider(x, y, w, h);
        }
        return null;
    }

    private static SlopeRegion? MakeRandomSlope(Random rng, Vector2 holePos)
    {
        int variant = rng.Next(4);
        return variant switch
        {
            0 => MakeUniformSlope(rng),
            1 => MakeFunnelSlope(rng, holePos),
            2 => MakeSwirlSlope(rng),
            _ => MakeSideGradientSlope(rng),
        };
    }

    private static SlopeRegion MakeUniformSlope(Random rng)
    {
        bool uphill = rng.Next(2) == 0;
        int x = rng.Next(FieldX + 100, FieldX + FieldW - 320);
        int y = rng.Next(FieldY + 100, FieldY + FieldH - 240);
        int w = rng.Next(180, 320);
        int h = rng.Next(100, 180);
        // Random axis-aligned direction (one of 4).
        int dir = rng.Next(4);
        Vector2 accel = dir switch
        {
            0 => new Vector2(0, 240),
            1 => new Vector2(0, -240),
            2 => new Vector2(240, 0),
            _ => new Vector2(-240, 0),
        };
        return new SlopeRegion(
            new Vector2(x, y), new Vector2(w, h), accel,
            uphill ? new Color(220, 60, 60, 170) : new Color(60, 200, 90, 160),
            uphill ? "UPHILL" : "DOWNHILL");
    }

    private static SlopeRegion MakeFunnelSlope(Random rng, Vector2 pivot)
    {
        int w = rng.Next(280, 460);
        int h = rng.Next(180, 320);
        int x = rng.Next(FieldX + 100, FieldX + FieldW - 100 - w);
        int y = rng.Next(FieldY + 100, FieldY + FieldH - 100 - h);
        int rows = rng.Next(3, 6), cols = rng.Next(4, 8);
        var cells = new Vector2[rows, cols];
        Vector2 cellSize = new((float)w / cols, (float)h / rows);
        Vector2 origin = new(x, y);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 cellCenter = origin + new Vector2((c + 0.5f) * cellSize.X, (r + 0.5f) * cellSize.Y);
                Vector2 to = pivot - cellCenter;
                float d = to.Length();
                if (d < 1f) { cells[r, c] = Vector2.Zero; continue; }
                cells[r, c] = (to / d) * MathHelper.Clamp(d * 0.5f, 80f, 240f);
            }
        }
        return new SlopeRegion(origin, new Vector2(w, h), Vector2.Zero,
            new Color(60, 180, 200, 90)) { Cells = cells };
    }

    private static SlopeRegion MakeSwirlSlope(Random rng)
    {
        int w = rng.Next(260, 420);
        int h = rng.Next(220, 360);
        int x = rng.Next(FieldX + 100, FieldX + FieldW - 100 - w);
        int y = rng.Next(FieldY + 100, FieldY + FieldH - 100 - h);
        int rows = rng.Next(4, 7), cols = rng.Next(4, 7);
        bool clockwise = rng.Next(2) == 0;
        float magnitude = rng.Next(140, 240);
        var cells = new Vector2[rows, cols];
        Vector2 cellSize = new((float)w / cols, (float)h / rows);
        Vector2 origin = new(x, y);
        Vector2 center = origin + new Vector2(w * 0.5f, h * 0.5f);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 cellCenter = origin + new Vector2((c + 0.5f) * cellSize.X, (r + 0.5f) * cellSize.Y);
                Vector2 radial = cellCenter - center;
                if (radial.LengthSquared() < 1f) { cells[r, c] = Vector2.Zero; continue; }
                radial.Normalize();
                // Rotate 90° to get tangent direction; flip sign for direction.
                Vector2 tangent = clockwise
                    ? new Vector2(-radial.Y, radial.X)
                    : new Vector2(radial.Y, -radial.X);
                cells[r, c] = tangent * magnitude;
            }
        }
        return new SlopeRegion(origin, new Vector2(w, h), Vector2.Zero,
            new Color(180, 100, 200, 90)) { Cells = cells };
    }

    private static SlopeRegion MakeSideGradientSlope(Random rng)
    {
        int w = rng.Next(280, 480);
        int h = rng.Next(140, 240);
        int x = rng.Next(FieldX + 100, FieldX + FieldW - 100 - w);
        int y = rng.Next(FieldY + 100, FieldY + FieldH - 100 - h);
        int rows = rng.Next(2, 5), cols = rng.Next(4, 8);
        bool horizontalAxis = rng.Next(2) == 0;
        Vector2 baseDir = horizontalAxis
            ? new Vector2(rng.Next(2) == 0 ? 1 : -1, 0)
            : new Vector2(0, rng.Next(2) == 0 ? 1 : -1);
        var cells = new Vector2[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Magnitude ramps across the cells along one axis.
                float t = horizontalAxis ? (float)c / (cols - 1) : (float)r / (rows - 1);
                float magnitude = MathHelper.Lerp(80f, 280f, t);
                cells[r, c] = baseDir * magnitude;
            }
        }
        return new SlopeRegion(new Vector2(x, y), new Vector2(w, h), Vector2.Zero,
            new Color(200, 160, 80, 110)) { Cells = cells };
    }

    private static Color RandomObstacleColor(Random rng) => new(
        (byte)rng.Next(120, 220),
        (byte)rng.Next(70, 170),
        (byte)rng.Next(40, 130));

    private static float DistanceFromPoint(Rectangle r, Vector2 p)
    {
        float cx = MathHelper.Clamp(p.X, r.Left, r.Right);
        float cy = MathHelper.Clamp(p.Y, r.Top, r.Bottom);
        return Vector2.Distance(p, new Vector2(cx, cy));
    }

    private static void AddOuterWalls(HoleLayout layout)
    {
        layout.Walls.Add(new AabbCollider(FieldX, FieldY, FieldW, WallThickness));               // top
        layout.Walls.Add(new AabbCollider(FieldX, FieldY + FieldH - WallThickness, FieldW, WallThickness)); // bottom
        layout.Walls.Add(new AabbCollider(FieldX, FieldY, WallThickness, FieldH));               // left
        layout.Walls.Add(new AabbCollider(FieldX + FieldW - WallThickness, FieldY, WallThickness, FieldH)); // right
    }

    private static Rectangle Inflate(Rectangle r, int amount) =>
        new(r.X - amount, r.Y - amount, r.Width + amount * 2, r.Height + amount * 2);

    private static AabbCollider? TryPlaceBox(Random rng, int w, int h,
        Rectangle teeBuffer, Vector2 holePos, int holeBuffer)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            int x = rng.Next(FieldX + 60, FieldX + FieldW - 60 - w);
            int y = rng.Next(FieldY + 60, FieldY + FieldH - 60 - h);
            var box = new Rectangle(x, y, w, h);
            if (box.Intersects(teeBuffer)) continue;
            if (Vector2.DistanceSquared(holePos, new Vector2(x + w / 2f, y + h / 2f)) < holeBuffer * holeBuffer) continue;
            return new AabbCollider(x, y, w, h);
        }
        return null;
    }
}
