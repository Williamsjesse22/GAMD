using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceRace.Graphics;

/// <summary>
/// Procedural mesh builders. Returns vertex + index arrays the caller can upload
/// to GPU buffers. All meshes are centered at the origin and built in the
/// MonoGame right-handed coordinate convention (X right, Y up, -Z forward).
/// </summary>
public static class MeshFactory
{
    /// <summary>Bundle of CPU-side mesh data ready to upload to <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>.</summary>
    public readonly struct MeshData
    {
        public readonly VertexPositionNormalTexture[] Vertices;
        public readonly short[] Indices;

        public MeshData(VertexPositionNormalTexture[] vertices, short[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        /// <summary>Upload to a fresh pair of GPU buffers. Caller owns disposal.</summary>
        public (VertexBuffer V, IndexBuffer I) ToGpu(GraphicsDevice device)
        {
            var v = new VertexBuffer(device, VertexPositionNormalTexture.VertexDeclaration, Vertices.Length, BufferUsage.None);
            v.SetData(Vertices);
            var i = new IndexBuffer(device, IndexElementSize.SixteenBits, Indices.Length, BufferUsage.None);
            i.SetData(Indices);
            return (v, i);
        }
    }

    /// <summary>
    /// Builds a torus around the Z axis (the ring's hole faces ±Z). With Identity
    /// orientation, the ring opens along Xna's forward axis (-Z), so a ship at the
    /// origin facing forward sees an upright hoop. <paramref name="majorRadius"/>
    /// is from torus center to tube center; <paramref name="minorRadius"/> is tube
    /// thickness.
    /// </summary>
    public static MeshData CreateTorus(float majorRadius, float minorRadius, int majorSegments = 32, int minorSegments = 12)
    {
        var verts = new VertexPositionNormalTexture[majorSegments * minorSegments];
        var idx = new short[majorSegments * minorSegments * 6];

        for (int i = 0; i < majorSegments; i++)
        {
            float u = (float)i / majorSegments;
            float theta = u * MathHelper.TwoPi;
            float cosT = MathF.Cos(theta), sinT = MathF.Sin(theta);
            // Ring centers lie in the XY plane (torus axis = Z).
            Vector3 ringCenter = new(majorRadius * cosT, majorRadius * sinT, 0f);

            for (int j = 0; j < minorSegments; j++)
            {
                float v = (float)j / minorSegments;
                float phi = v * MathHelper.TwoPi;
                float cosP = MathF.Cos(phi), sinP = MathF.Sin(phi);

                // Tube cross-section: spanned by the radial outward and Z.
                Vector3 outward = new(cosT, sinT, 0f);
                Vector3 normal = outward * cosP + Vector3.UnitZ * sinP;
                Vector3 position = ringCenter + normal * minorRadius;

                verts[i * minorSegments + j] = new VertexPositionNormalTexture(position, normal, new Vector2(u, v));
            }
        }

        int t = 0;
        for (int i = 0; i < majorSegments; i++)
        {
            int iN = (i + 1) % majorSegments;
            for (int j = 0; j < minorSegments; j++)
            {
                int jN = (j + 1) % minorSegments;
                short a = (short)(i * minorSegments + j);
                short b = (short)(iN * minorSegments + j);
                short c = (short)(iN * minorSegments + jN);
                short d = (short)(i * minorSegments + jN);
                idx[t++] = a; idx[t++] = b; idx[t++] = c;
                idx[t++] = a; idx[t++] = c; idx[t++] = d;
            }
        }

        return new MeshData(verts, idx);
    }

    /// <summary>
    /// A simple low-poly arrowhead-shaped ship: 5 vertices forming a triangular prism
    /// with a pointed nose, oriented along -Z (forward in Xna's right-handed convention).
    /// Body is roughly 2 units long, 1.2 wide, 0.6 tall.
    /// </summary>
    public static MeshData CreateShipWedge()
    {
        // Vertices (4 hull + 1 nose) — duplicate each face's vertices so normals are flat.
        // Layout: nose at -Z; tail rectangle at +Z.
        Vector3 nose = new(0f, 0f, -1.0f);
        Vector3 tlt = new(-0.6f, 0.3f, 0.5f);   // tail-left-top
        Vector3 trt = new(0.6f, 0.3f, 0.5f);    // tail-right-top
        Vector3 tlb = new(-0.6f, -0.3f, 0.5f);  // tail-left-bottom
        Vector3 trb = new(0.6f, -0.3f, 0.5f);   // tail-right-bottom

        // 6 triangular faces: top-left, top-right, bottom-left, bottom-right, tail (2 tris).
        var verts = new VertexPositionNormalTexture[18];
        var idx = new short[18];
        int v = 0, t = 0;

        void AddTri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            verts[v + 0] = new VertexPositionNormalTexture(a, n, Vector2.Zero);
            verts[v + 1] = new VertexPositionNormalTexture(b, n, Vector2.UnitX);
            verts[v + 2] = new VertexPositionNormalTexture(c, n, Vector2.One);
            idx[t++] = (short)(v + 0);
            idx[t++] = (short)(v + 1);
            idx[t++] = (short)(v + 2);
            v += 3;
        }

        // Top-left face (nose, top-right, top-left).
        AddTri(nose, trt, tlt);
        // Top-right face — collapsed; instead split top into one tri above, then bottom-left and bottom-right faces.
        // Bottom face (nose, bottom-left, bottom-right).
        AddTri(nose, tlb, trb);
        // Left face (nose, top-left, bottom-left).
        AddTri(nose, tlt, tlb);
        // Right face (nose, bottom-right, top-right).
        AddTri(nose, trb, trt);
        // Tail face (top-left, top-right, bottom-left).
        AddTri(tlt, trt, tlb);
        // Tail face (top-right, bottom-right, bottom-left).
        AddTri(trt, trb, tlb);

        return new MeshData(verts, idx);
    }

    /// <summary>
    /// An inverted cube — same as a unit cube, but with face winding reversed so that
    /// the cube's *interior* is lit when seen from inside. Used for skyboxes.
    /// </summary>
    public static MeshData CreateInvertedCube(float size)
    {
        float h = size * 0.5f;
        // 8 corners; 6 faces × 2 tris × 3 verts = 36 indices.
        Vector3[] c = {
            new(-h, -h, -h), new( h, -h, -h), new( h,  h, -h), new(-h,  h, -h),
            new(-h, -h,  h), new( h, -h,  h), new( h,  h,  h), new(-h,  h,  h),
        };
        // Faces with reversed winding so interior is the visible side.
        (int A, int B, int C, int D, Vector3 N)[] faces = {
            (1, 0, 3, 2, new Vector3(0, 0,  1)), // -Z face seen from inside → normal +Z
            (4, 5, 6, 7, new Vector3(0, 0, -1)),
            (0, 4, 7, 3, new Vector3( 1, 0, 0)),
            (5, 1, 2, 6, new Vector3(-1, 0, 0)),
            (3, 7, 6, 2, new Vector3(0, -1, 0)),
            (4, 0, 1, 5, new Vector3(0,  1, 0)),
        };

        var verts = new VertexPositionNormalTexture[24];
        var idx = new short[36];
        int vi = 0, ti = 0;
        foreach (var f in faces)
        {
            verts[vi + 0] = new VertexPositionNormalTexture(c[f.A], f.N, new Vector2(0, 0));
            verts[vi + 1] = new VertexPositionNormalTexture(c[f.B], f.N, new Vector2(1, 0));
            verts[vi + 2] = new VertexPositionNormalTexture(c[f.C], f.N, new Vector2(1, 1));
            verts[vi + 3] = new VertexPositionNormalTexture(c[f.D], f.N, new Vector2(0, 1));
            idx[ti++] = (short)(vi + 0); idx[ti++] = (short)(vi + 1); idx[ti++] = (short)(vi + 2);
            idx[ti++] = (short)(vi + 0); idx[ti++] = (short)(vi + 2); idx[ti++] = (short)(vi + 3);
            vi += 4;
        }
        return new MeshData(verts, idx);
    }

    /// <summary>Solid sphere mesh, useful for debug rendering and the placeholder ship.</summary>
    public static MeshData CreateSphere(float radius, int latitudeBands = 12, int longitudeBands = 18)
    {
        int vertCount = (latitudeBands + 1) * (longitudeBands + 1);
        var verts = new VertexPositionNormalTexture[vertCount];
        int v = 0;
        for (int lat = 0; lat <= latitudeBands; lat++)
        {
            float theta = lat * MathHelper.Pi / latitudeBands;
            float sinT = MathF.Sin(theta), cosT = MathF.Cos(theta);
            for (int lon = 0; lon <= longitudeBands; lon++)
            {
                float phi = lon * MathHelper.TwoPi / longitudeBands;
                float sinP = MathF.Sin(phi), cosP = MathF.Cos(phi);
                Vector3 normal = new(cosP * sinT, cosT, sinP * sinT);
                verts[v++] = new VertexPositionNormalTexture(
                    normal * radius,
                    normal,
                    new Vector2((float)lon / longitudeBands, (float)lat / latitudeBands));
            }
        }

        var idx = new short[latitudeBands * longitudeBands * 6];
        int t = 0;
        for (int lat = 0; lat < latitudeBands; lat++)
        {
            for (int lon = 0; lon < longitudeBands; lon++)
            {
                int first = lat * (longitudeBands + 1) + lon;
                int second = first + longitudeBands + 1;
                idx[t++] = (short)first;
                idx[t++] = (short)second;
                idx[t++] = (short)(first + 1);
                idx[t++] = (short)second;
                idx[t++] = (short)(second + 1);
                idx[t++] = (short)(first + 1);
            }
        }
        return new MeshData(verts, idx);
    }
}
