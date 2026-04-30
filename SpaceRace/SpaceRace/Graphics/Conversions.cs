using BepuPhysics;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace SpaceRace.Graphics;

/// <summary>
/// Bepu uses <see cref="System.Numerics"/> types; MonoGame uses
/// <c>Microsoft.Xna.Framework</c> types. They have the same memory layout but
/// the C# type system treats them as distinct. These helpers bridge the two
/// at the rendering boundary, where the body's physical pose becomes a draw matrix.
/// </summary>
public static class Conversions
{
    public static XnaVector3 ToXna(this NumVector3 v) => new(v.X, v.Y, v.Z);
    public static NumVector3 ToNumerics(this XnaVector3 v) => new(v.X, v.Y, v.Z);

    public static XnaQuaternion ToXna(this NumQuaternion q) => new(q.X, q.Y, q.Z, q.W);
    public static NumQuaternion ToNumerics(this XnaQuaternion q) => new(q.X, q.Y, q.Z, q.W);

    /// <summary>Convert a Bepu rigid pose to an Xna world matrix (rotation × translation).</summary>
    public static XnaMatrix ToWorldMatrix(this RigidPose pose)
    {
        return XnaMatrix.CreateFromQuaternion(pose.Orientation.ToXna())
             * XnaMatrix.CreateTranslation(pose.Position.ToXna());
    }
}
