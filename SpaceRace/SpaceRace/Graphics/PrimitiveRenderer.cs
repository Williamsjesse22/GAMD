using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceRace.Graphics;

/// <summary>
/// Thin <see cref="BasicEffect"/> wrapper for drawing meshes with flat lighting.
/// Owns its <see cref="BasicEffect"/> instance and a default light setup; callers
/// supply a world matrix per draw plus the active camera's view/projection.
/// </summary>
public sealed class PrimitiveRenderer
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;

    public PrimitiveRenderer(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            LightingEnabled = true,
            VertexColorEnabled = false,
            TextureEnabled = false,
            PreferPerPixelLighting = false,
        };
        _effect.EnableDefaultLighting();
        _effect.AmbientLightColor = new Vector3(0.25f, 0.27f, 0.35f);
    }

    /// <summary>Camera state: call once per frame before any DrawMesh calls.</summary>
    public void SetCamera(Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
    }

    /// <summary>Draw a mesh at <paramref name="world"/> with the given diffuse color.</summary>
    public void DrawMesh(VertexBuffer vertices, IndexBuffer indices, Matrix world, Color diffuse)
    {
        _effect.World = world;
        _effect.DiffuseColor = diffuse.ToVector3();
        _effect.EmissiveColor = Vector3.Zero;
        _effect.Alpha = 1f;
        DrawWithEffect(vertices, indices);
    }

    /// <summary>
    /// Glow pass: render the mesh again with emissive color + additive blend, no
    /// lighting, scaled slightly to create a fake rim glow. Used to accent the
    /// active ring.
    /// </summary>
    public void DrawGlow(VertexBuffer vertices, IndexBuffer indices, Matrix world, Color glowColor, float scale = 1.06f)
    {
        Matrix scaled = Matrix.CreateScale(scale) * world;
        var prevBlend = _device.BlendState;
        var prevDepth = _device.DepthStencilState;

        _device.BlendState = BlendState.Additive;
        _device.DepthStencilState = DepthStencilState.DepthRead;

        bool prevLighting = _effect.LightingEnabled;
        _effect.LightingEnabled = false;
        _effect.World = scaled;
        _effect.DiffuseColor = Vector3.Zero;
        _effect.EmissiveColor = glowColor.ToVector3();
        _effect.Alpha = 0.55f;
        DrawWithEffect(vertices, indices);

        _effect.LightingEnabled = prevLighting;
        _effect.Alpha = 1f;
        _device.BlendState = prevBlend;
        _device.DepthStencilState = prevDepth;
    }

    private void DrawWithEffect(VertexBuffer vertices, IndexBuffer indices)
    {
        _device.SetVertexBuffer(vertices);
        _device.Indices = indices;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indices.IndexCount / 3);
        }
    }
}
