using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceRace.Systems;

/// <summary>
/// Creates simple solid-color textures at runtime so the game runs without external
/// PNG assets. A 1x1 white pixel covers all rectangle drawing (tinted via SpriteBatch);
/// the ball gets a small filled-circle texture so it visually reads as round.
/// </summary>
public static class TextureFactory
{
    /// <summary>Returns a 1x1 white pixel suitable for tinting in SpriteBatch.Draw.</summary>
    public static Texture2D CreatePixel(GraphicsDevice device)
    {
        var tex = new Texture2D(device, 1, 1);
        tex.SetData(new[] { Color.White });
        return tex;
    }

    /// <summary>
    /// Returns a filled circle of the given radius, white, with anti-aliased edge
    /// (alpha blended over a 1-pixel band). Tint via SpriteBatch.Draw color.
    /// </summary>
    public static Texture2D CreateCircle(GraphicsDevice device, int radius)
    {
        int diameter = radius * 2;
        var tex = new Texture2D(device, diameter, diameter);
        var data = new Color[diameter * diameter];

        float r = radius;
        float r2 = r * r;
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float d2 = dx * dx + dy * dy;

                if (d2 <= (r - 1f) * (r - 1f))
                {
                    data[y * diameter + x] = Color.White;
                }
                else if (d2 <= r2)
                {
                    float dist = System.MathF.Sqrt(d2);
                    float alpha = MathHelper.Clamp(r - dist, 0f, 1f);
                    data[y * diameter + x] = Color.White * alpha;
                }
                else
                {
                    data[y * diameter + x] = Color.Transparent;
                }
            }
        }

        tex.SetData(data);
        return tex;
    }

    /// <summary>
    /// Generates a single skybox face: a square texture of <paramref name="size"/>×<paramref name="size"/>
    /// pixels with <paramref name="starCount"/> randomly placed stars on a deep-space background.
    /// Each star is rendered as a 3×3 cluster (bright center + 8 half-bright neighbors)
    /// so it survives MonoGame's default linear-filtering downsampling without
    /// disappearing into the dark background. Brightest ~30% additionally get a
    /// faint cross halo for variety.
    /// </summary>
    public static Texture2D CreateStarfieldFace(GraphicsDevice device, int size, int starCount, Random rng)
    {
        var data = new Color[size * size];
        Color background = new(8, 10, 22);
        for (int i = 0; i < data.Length; i++) data[i] = background;

        for (int s = 0; s < starCount; s++)
        {
            int x = rng.Next(1, size - 1);
            int y = rng.Next(1, size - 1);
            byte b = (byte)rng.Next(140, 256);
            byte rTint = (byte)Math.Clamp(b + rng.Next(-30, 31), 0, 255);
            byte gTint = (byte)Math.Clamp(b + rng.Next(-30, 21), 0, 255);

            // Single bright pixel — point-clamp sampling preserves it crisply.
            data[y * size + x] = new Color(rTint, gTint, b);

            // ~25% of stars get a faint 4-pixel cross halo, only the bright ones.
            if (b > 200 && rng.Next(4) == 0)
            {
                byte h = (byte)(b / 3);
                Color halo = new(h, h, h);
                data[y * size + x - 1] = halo;
                data[y * size + x + 1] = halo;
                data[(y - 1) * size + x] = halo;
                data[(y + 1) * size + x] = halo;
            }
        }

        var tex = new Texture2D(device, size, size);
        tex.SetData(data);
        return tex;
    }
}
