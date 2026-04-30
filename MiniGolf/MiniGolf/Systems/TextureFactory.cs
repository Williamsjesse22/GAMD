using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniGolf.Systems;

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
}
