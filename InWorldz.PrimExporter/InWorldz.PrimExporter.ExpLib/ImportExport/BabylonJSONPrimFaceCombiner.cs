using System;
using System.Collections.Generic;
using System.Linq;
using InWorldz.PrimExporter.ExpLib.PrimFace;
using OpenMetaverse;

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

            List<Tuple<Vector3, Vector2, Vector3>> newVertsWithUvsAndNormals = new List<Tuple<Vector3, Vector2, Vector3>>();
            for (int i = 0; i < faceData.Indices.Length; i += 3)
            {
                ushort a = (ushort)(faceData.Indices[i] + (verticesBase / 3));
                ushort b = (ushort)(faceData.Indices[i + 1] + (verticesBase / 3));
                ushort c = (ushort)(faceData.Indices[i + 2] + (verticesBase / 3));

                //insert as C, B, A
                //swap UVs between C and A
                newVertsWithUvsAndNormals.Add(new Tuple<Vector3, Vector2, Vector3>(
                    new Vector3(faceData.Vertices[c * 3], faceData.Vertices[c * 3 + 1], faceData.Vertices[c * 3 + 2]), 
                    new Vector2(faceData.TexCoords[a * 2], faceData.TexCoords[a * 2 + 1]),
                    new Vector3(faceData.Normals[c * 3], faceData.Normals[c * 3 + 1], faceData.Normals[c * 3 + 2])
                    ));

                newVertsWithUvsAndNormals.Add(new Tuple<Vector3, Vector2, Vector3>(
                    new Vector3(faceData.Vertices[b * 3], faceData.Vertices[b * 3 + 1], faceData.Vertices[b * 3 + 2]),
                    new Vector2(faceData.TexCoords[b * 2], faceData.TexCoords[b * 2 + 1]),
                    new Vector3(faceData.Normals[b * 3], faceData.Normals[b * 3 + 1], faceData.Normals[b * 3 + 2])
                    ));

                newVertsWithUvsAndNormals.Add(new Tuple<Vector3, Vector2, Vector3>(
                    new Vector3(faceData.Vertices[a * 3], faceData.Vertices[a * 3 + 1], faceData.Vertices[a * 3 + 2]),
                    new Vector2(faceData.TexCoords[c * 2], faceData.TexCoords[c * 2 + 1]),
                    new Vector3(faceData.Normals[a * 3], faceData.Normals[a * 3 + 1], faceData.Normals[a * 3 + 2])
                    ));
            }

            //reindex
            Dictionary<Tuple<Vector3, Vector2, Vector3>, ushort> indexedFaces 
                = new Dictionary<Tuple<Vector3, Vector2, Vector3>, ushort>();

            List<ushort> newIndexes = new List<ushort>();
            List<Tuple<Vector3, Vector2, Vector3>> deduplicatedVertices = new List<Tuple<Vector3, Vector2, Vector3>>();

            for (int i = 0; i < newVertsWithUvsAndNormals.Count; i++)
            {
                var vert = newVertsWithUvsAndNormals[i];

                //see if we have this vert indexed already
                ushort existingIndex;
                if (indexedFaces.TryGetValue(vert, out existingIndex))
                {
                    //yes, add the existing index to the new index list
                    newIndexes.Add(existingIndex);
                }
                else
                {
                    //no, we need to add this vertex and index
                    var idxPos = deduplicatedVertices.Count;
                    indexedFaces.Add(vert, (ushort)idxPos);
                    deduplicatedVertices.Add(vert);
                    newIndexes.Add((ushort)(indexBase + idxPos));
                }
            }

            //dump the new vertices, normals and UVs
            Vertices.AddRange(deduplicatedVertices.SelectMany(v => new [] { v.Item1.X, v.Item1.Y, v.Item1.Z }));
            Normals.AddRange(deduplicatedVertices.SelectMany(v => new[] { v.Item3.X, v.Item3.Y, v.Item3.Z }));
            UVs.AddRange(deduplicatedVertices.SelectMany(v => new[] { v.Item2.X, v.Item2.Y}));

            Indices.AddRange(newIndexes);

            //dump a material for the entire VIEWER FACE
            Materials.Add(face.TextureFace);

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
