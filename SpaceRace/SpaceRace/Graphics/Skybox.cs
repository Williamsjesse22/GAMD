using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceRace.Systems;

namespace SpaceRace.Graphics;

/// <summary>
/// 6-textured-quad skybox. Each face is a procedural starfield generated at
/// construction; the cube is centered on the active camera each frame and drawn
/// before world geometry with depth disabled so it never occludes anything.
/// </summary>
/// <remarks>
/// All GPU resources are created in the constructor (not <c>LoadContent</c>) to
/// match the lifecycle pattern used by <c>Ship</c> and <c>Ring</c>; in this
/// project's host-component setup, <c>LoadContent</c> never fires for components
/// added during <c>Game1.LoadContent</c>, so anything created there silently
/// stays null and the component renders nothing.
/// </remarks>
public sealed class Skybox : DrawableGameComponent
{
    private readonly Camera _camera;
    private readonly VertexBuffer _vb;
    private readonly IndexBuffer _ib;
    private readonly BasicEffect _effect;
    private readonly Texture2D[] _faceTextures = new Texture2D[6];
    private readonly Vector3[] _faceCenters = new Vector3[6];
    private readonly Vector3[] _faceUAxes = new Vector3[6];
    private readonly Vector3[] _faceVAxes = new Vector3[6];
    private readonly VertexPositionTexture[] _quadVerts = new VertexPositionTexture[4];

    /// <summary>Half-extent of the skybox cube in world units.</summary>
    public float Size { get; }

    public Skybox(Game game, Camera camera, float size = 500f) : base(game)
    {
        _camera = camera;
        Size = size;
        DrawOrder = -1000;

        InitFaceBasis();

        var rng = new Random(1337);
        const int faceSize = 512;
        const int starsPerFace = 3000;
        for (int f = 0; f < 6; f++)
            _faceTextures[f] = TextureFactory.CreateStarfieldFace(GraphicsDevice, faceSize, starsPerFace, rng);

        // Single reusable quad VB; positions are rewritten per-face inside Draw.
        _vb = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.None);
        _vb.SetData(new VertexPositionTexture[4]);

        _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.None);
        _ib.SetData(new short[] { 0, 1, 2, 0, 2, 3 });

        _effect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = false,
        };
    }

    private void InitFaceBasis()
    {
        // Order: +X, -X, +Y, -Y, +Z, -Z. U,V pick consistent winding for each face.
        _faceCenters[0] = Vector3.UnitX;  _faceUAxes[0] = -Vector3.UnitZ; _faceVAxes[0] =  Vector3.UnitY;
        _faceCenters[1] = -Vector3.UnitX; _faceUAxes[1] =  Vector3.UnitZ; _faceVAxes[1] =  Vector3.UnitY;
        _faceCenters[2] = Vector3.UnitY;  _faceUAxes[2] =  Vector3.UnitX; _faceVAxes[2] =  Vector3.UnitZ;
        _faceCenters[3] = -Vector3.UnitY; _faceUAxes[3] =  Vector3.UnitX; _faceVAxes[3] = -Vector3.UnitZ;
        _faceCenters[4] = Vector3.UnitZ;  _faceUAxes[4] =  Vector3.UnitX; _faceVAxes[4] =  Vector3.UnitY;
        _faceCenters[5] = -Vector3.UnitZ; _faceUAxes[5] = -Vector3.UnitX; _faceVAxes[5] =  Vector3.UnitY;
    }

    public override void Draw(GameTime gameTime)
    {
        var prevDepth = GraphicsDevice.DepthStencilState;
        var prevRaster = GraphicsDevice.RasterizerState;
        var prevSampler = GraphicsDevice.SamplerStates[0];
        var prevBlend = GraphicsDevice.BlendState;

        // Don't write to depth, render both sides (saves us from worrying about
        // winding direction on the cube interior), point-sample to keep stars sharp.
        GraphicsDevice.DepthStencilState = DepthStencilState.None;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        GraphicsDevice.BlendState = BlendState.Opaque;

        _effect.View = _camera.View;
        _effect.Projection = _camera.Projection;
        _effect.World = Matrix.CreateTranslation(_camera.Position);

        for (int f = 0; f < 6; f++)
        {
            UpdateFaceVertices(f);
            _effect.Texture = _faceTextures[f];
            GraphicsDevice.SetVertexBuffer(_vb);
            GraphicsDevice.Indices = _ib;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
            }
        }

        GraphicsDevice.DepthStencilState = prevDepth;
        GraphicsDevice.RasterizerState = prevRaster;
        GraphicsDevice.SamplerStates[0] = prevSampler;
        GraphicsDevice.BlendState = prevBlend;
    }

    private void UpdateFaceVertices(int faceIndex)
    {
        Vector3 c = _faceCenters[faceIndex] * Size;
        Vector3 u = _faceUAxes[faceIndex] * Size;
        Vector3 v = _faceVAxes[faceIndex] * Size;
        _quadVerts[0] = new VertexPositionTexture(c - u - v, new Vector2(0, 1));
        _quadVerts[1] = new VertexPositionTexture(c + u - v, new Vector2(1, 1));
        _quadVerts[2] = new VertexPositionTexture(c + u + v, new Vector2(1, 0));
        _quadVerts[3] = new VertexPositionTexture(c - u + v, new Vector2(0, 0));
        _vb.SetData(_quadVerts);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vb.Dispose();
            _ib.Dispose();
            _effect.Dispose();
            for (int i = 0; i < _faceTextures.Length; i++) _faceTextures[i]?.Dispose();
        }
        base.Dispose(disposing);
    }
}
