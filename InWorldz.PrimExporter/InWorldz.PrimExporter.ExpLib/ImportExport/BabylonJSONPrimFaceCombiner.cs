using System;
using System.Collections.Generic;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class BabylonJSONPrimFaceCombiner
    {
        public class SubmeshDesc
        {
            public int MaterialIndex { get; set; }
            public int VerticesStart { get; set; }
            public int VerticesCount { get; set; }
            public int IndexStart { get; set; }
            public int IndexCount { get; set; }
        }


        public List<ushort> Indices = new List<ushort>();
        public List<float> Vertices = new List<float>();
        public List<float> Normals = new List<float>();
        public List<float> UVs = new List<float>();
        public List<OpenMetaverse.Primitive.TextureEntryFace> Materials = new List<OpenMetaverse.Primitive.TextureEntryFace>();
        public List<SubmeshDesc> SubMeshes = new List<SubmeshDesc>();
        
        public void CombineFace(OpenMetaverse.Rendering.Face face)
        {
            int verticesBase = Vertices.Count;
            int materialBase = Materials.Count;
            int indexBase = Indices.Count;

            PrimFace.FaceData faceData = (PrimFace.FaceData)face.UserData;

            //dump the vertices as they are
            Vertices.AddRange(faceData.Vertices);
            Normals.AddRange(faceData.Normals);
            UVs.AddRange(faceData.TexCoords);
                
            //dump a material for the entire VIEWER FACE
            Materials.Add(face.TextureFace);

            for (int i = 0; i < faceData.Indices.Length; i += 3)
            {
                ushort a = (ushort)(faceData.Indices[i] + (verticesBase / 3));
                ushort b = (ushort)(faceData.Indices[i + 1] + (verticesBase / 3));
                ushort c = (ushort)(faceData.Indices[i + 2] + (verticesBase / 3));

                Indices.Add(a);
                Indices.Add(b);
                Indices.Add(c);
            }

            SubMeshes.Add(
                new SubmeshDesc
                {
                    MaterialIndex = materialBase,
                    VerticesStart = verticesBase,
                    VerticesCount = Vertices.Count - verticesBase,
                    IndexStart = indexBase,
                    IndexCount = Indices.Count - indexBase
                });
        }
    }
}
