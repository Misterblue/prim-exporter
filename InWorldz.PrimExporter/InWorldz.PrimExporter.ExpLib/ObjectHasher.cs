using System;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using OpenSim.Framework;

namespace InWorldz.PrimExporter.ExpLib
{
    public sealed class ObjectHasher
    {
        static ObjectHasher()
        {
        }

        /// <summary>
        /// Calculate a hash value over fields that can affect the underlying physics shape.
        /// Things like RenderMaterials and TextureEntry data are not included.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="lod"></param>
        /// <returns>ulong - a calculated hash value</returns>
        public ulong GetMeshShapeHash(PrimitiveBaseShape shape, DetailLevel lod)
        {
            ulong hash = 5381;

            hash = djb2(hash, shape.PathCurve);
            hash = djb2(hash, (byte)((byte)shape.HollowShape | (byte)shape.ProfileShape));
            hash = djb2(hash, shape.PathBegin);
            hash = djb2(hash, shape.PathEnd);
            hash = djb2(hash, shape.PathScaleX);
            hash = djb2(hash, shape.PathScaleY);
            hash = djb2(hash, shape.PathShearX);
            hash = djb2(hash, shape.PathShearY);
            hash = djb2(hash, (byte)shape.PathTwist);
            hash = djb2(hash, (byte)shape.PathTwistBegin);
            hash = djb2(hash, (byte)shape.PathRadiusOffset);
            hash = djb2(hash, (byte)shape.PathTaperX);
            hash = djb2(hash, (byte)shape.PathTaperY);
            hash = djb2(hash, shape.PathRevolutions);
            hash = djb2(hash, (byte)shape.PathSkew);
            hash = djb2(hash, shape.ProfileBegin);
            hash = djb2(hash, shape.ProfileEnd);
            hash = djb2(hash, shape.ProfileHollow);

            // Include LOD in hash, accounting for endianness
            byte[] lodBytes = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes((int)lod), 0, lodBytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lodBytes, 0, 4);
            }

            foreach (byte t in lodBytes)
                hash = djb2(hash, t);

            // include sculpt UUID
            if (shape.SculptEntry)
            {
                var sculptUUIDBytes = shape.SculptTexture.GetBytes();
                foreach (byte t in sculptUUIDBytes)
                    hash = djb2(hash, t);

                hash = djb2(hash, shape.SculptType);
            }

            return hash;
        }

        /// <summary>
        /// Returns a hash value calculated from face parameters that would affect
        /// the appearance of the mesh faces but not their shape
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        public ulong GetMeshMaterialHash(FacetedMesh mesh, Primitive prim)
        {
            ulong hash = 5381;

            var numFaces = mesh.Faces.Count;
            for (int i = 0; i < numFaces; i++)
            {
                Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)i);
                hash = GetMaterialFaceHash(hash, teFace);
            }

            return hash;
        }

        public ulong GetMaterialFaceHash(ulong hash, Primitive.TextureEntryFace teFace)
        {
            hash = djb2(hash, (ushort) teFace.Bump);
            hash = djb2(hash, (byte) (teFace.Fullbright ? 1 : 0));
            hash = djb2(hash, BitConverter.GetBytes(teFace.Glow));
            hash = djb2(hash, (byte) (teFace.MediaFlags ? 1 : 0));
            hash = djb2(hash, BitConverter.GetBytes(teFace.OffsetU));
            hash = djb2(hash, BitConverter.GetBytes(teFace.OffsetV));
            hash = djb2(hash, BitConverter.GetBytes(teFace.RepeatU));
            hash = djb2(hash, BitConverter.GetBytes(teFace.RepeatV));
            hash = djb2(hash, BitConverter.GetBytes(teFace.Rotation));
            hash = djb2(hash, teFace.RGBA.GetBytes());
            hash = djb2(hash, (byte) teFace.Shiny);
            hash = djb2(hash, (byte) teFace.TexMapType);
            hash = djb2(hash, teFace.TextureID.GetBytes());
            return hash;
        }

        public ulong GetMaterialFaceHash(Primitive.TextureEntryFace teFace)
        {
            ulong hash = 5381;
            return GetMaterialFaceHash(hash, teFace);
        }

        private ulong djb2(ulong hash, byte c)
        {
            return ((hash << 5) + hash) + (ulong)c;
        }

        private ulong djb2(ulong hash, ushort c)
        {
            hash = ((hash << 5) + hash) + (ulong)((byte)c);
            return ((hash << 5) + hash) + (ulong)(c >> 8);
        }

        private ulong djb2(ulong hash, byte[] bytes)
        {
            foreach (byte b in bytes)
                hash = djb2(hash, b);

            return hash;
        }
    }
}