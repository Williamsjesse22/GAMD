using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MiniGolf.Physics;
using MiniGolf.Physics.Colliders;

namespace MiniGolf.GameObjects;

/// <summary>
/// Pure data describing a single mini-golf hole. Constructed by hand-crafted
/// builders in <see cref="HoleFactory"/> (Classic, Funnel, ...) or generated
/// procedurally; consumed by <see cref="Course.LoadLayout"/>.
/// </summary>
public sealed class HoleLayout
{
    /// <summary>Display name (e.g. "Hole 1: Classic"). Optional.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Background fairway rectangle in world pixels. Used as the single fairway
    /// region when <see cref="FairwayRegions"/> is empty (most holes), and as
    /// the camera/HUD bounding box for all holes.
    /// </summary>
    public Rectangle Fairway { get; init; }

    /// <summary>
    /// Optional multi-rectangle fairway. When non-empty, the green grass is drawn
    /// from THIS list instead of <see cref="Fairway"/>, allowing non-rectangular
    /// hole shapes (L, T, +, etc.) composed of overlapping or adjacent rectangles.
    /// The walls in <see cref="Walls"/> still constrain the ball — make sure the
    /// fairway regions match the walled-in playable area.
    /// </summary>
    public List<Rectangle> FairwayRegions { get; } = new();

    /// <summary>Wall rectangles (outer + inner dividers). Drawn in the wall color.</summary>
    public List<AabbCollider> Walls { get; } = new();

    /// <summary>Obstacle rectangles, with their per-instance fill color.</summary>
    public List<(AabbCollider Box, Color Color)> Obstacles { get; } = new();

    /// <summary>Slope regions (uniform or grid).</summary>
    public List<SlopeRegion> Slopes { get; } = new();

    /// <summary>Tee rectangle — the blue spawn point for the ball.</summary>
    public Rectangle TeeRect { get; set; }

    /// <summary>Hole center in world pixels.</summary>
    public Vector2 HolePosition { get; set; }

    /// <summary>Hole capture radius.</summary>
    public float HoleRadius { get; init; } = 14f;

    /// <summary>Maximum ball speed at which the hole accepts the ball.</summary>
    public float HoleMaxEntrySpeed { get; init; } = 320f;
}
