using System.Collections.Generic;
using System.Linq;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class BabylonJSONPrimFaceCombiner
    {
        private readonly ObjectHasher _objHasher = new ObjectHasher();

        public class SubmeshDesc
        {
            public ulong MaterialHash { get; set; }
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

            //negate X on the vertices and normals to account for a
            //different coordinate space
            for (int i = 0; i < faceData.Vertices.Length; i++)
            {
                if (i % 3 == 0)
                {
                    Vertices.Add(-faceData.Vertices[i]);
                }
                else
                {
                    Vertices.Add(faceData.Vertices[i]);
                }
            }

            for (int i = 0; i < faceData.Normals.Length; i++)
            {
                if (i % 3 == 0)
                {
                    Normals.Add(-faceData.Normals[i]);
                }
                else
                {
                    Normals.Add(faceData.Normals[i]);
                }
            }
            
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

            //if this submesh and the last are using identical materials, 
            //combine them into a single face
            ulong matHash = _objHasher.GetMaterialFaceHash(face.TextureFace);
            var lastSubMesh = SubMeshes.Last();
            if (SubMeshes.Count > 0 &&  lastSubMesh.MaterialHash == matHash)
            {
                //combine.. just add to the vertex/index counts
                lastSubMesh.VerticesCount += Vertices.Count - verticesBase;
                lastSubMesh.IndexCount += Indices.Count - indexBase;
            }
            else
            {
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
}
