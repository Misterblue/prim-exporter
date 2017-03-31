// Copyright 2016 InWorldz Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.Rendering;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class BabylonPrimFaceCombiner
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

        /// <summary>
        /// Comparer for comparing two keys, handling equality as beeing greater
        /// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        public class DuplicateKeyComparer<TKey>
                        :
                     IComparer<TKey> where TKey : IComparable
        {
            #region IComparer<TKey> Members

            public int Compare(TKey x, TKey y)
            {
                int result = x.CompareTo(y);

                if (result == 0)
                    return 1;   // Handle equality as beeing greater
                else
                    return result;
            }

            #endregion
        }

        public List<ushort> Indices = new List<ushort>();
        public List<float> Vertices = new List<float>();
        public List<float> Normals = new List<float>();
        public List<float> UVs = new List<float>();
        public List<Primitive.TextureEntryFace> Materials = new List<Primitive.TextureEntryFace>();
        public List<SubmeshDesc> SubMeshes = new List<SubmeshDesc>();

        private SortedList<int, Face> _sortedFaces = new SortedList<int, Face>(new DuplicateKeyComparer<int>());
        private Dictionary<ulong, int> _knownMaterials = new Dictionary<ulong, int>();

        public void CombineFace(Face face)
        {
            ulong matHash = _objHasher.GetMaterialFaceHash(face.TextureFace);
            
            int matIdx;
            if (! _knownMaterials.TryGetValue(matHash, out matIdx))
            {
                _knownMaterials.Add(matHash, Materials.Count);
                matIdx = Materials.Count;

                Materials.Add(face.TextureFace);
            }

            _sortedFaces.Add(matIdx, face);
        }

        public void Complete()
        {
            List<Tuple<Vector3, Vector2, Vector3, int>> newVertsWithUvsAndNormalsAndMaterial =
                    new List<Tuple<Vector3, Vector2, Vector3, int>>();

            CreateVertexCloud(newVertsWithUvsAndNormalsAndMaterial);

            //reindex
            Dictionary<Tuple<Vector3, Vector2, Vector3, int>, ushort> indexedFaces
                = new Dictionary<Tuple<Vector3, Vector2, Vector3, int>, ushort>();

            List<ushort> newIndexes = new List<ushort>();
            List<Tuple<Vector3, Vector2, Vector3, int>> deduplicatedVertices 
                = new List<Tuple<Vector3, Vector2, Vector3, int>>();

            int lastMat = newVertsWithUvsAndNormalsAndMaterial[0].Item4;
            int vertStart = 0;
            int indexStart = 0;
            for (int i = 0; i < newVertsWithUvsAndNormalsAndMaterial.Count; i++)
            {
                var vert = newVertsWithUvsAndNormalsAndMaterial[i];

                if (lastMat != vert.Item4)
                {   
                    //material has changed, create and add a new submesh here
                    SubMeshes.Add(
                        new SubmeshDesc
                        {
                            MaterialIndex = lastMat,
                            VerticesStart = vertStart,
                            VerticesCount = deduplicatedVertices.Count - vertStart,
                            IndexStart = indexStart,
                            IndexCount = newIndexes.Count - indexStart
                        });

                    lastMat = vert.Item4;
                    vertStart = deduplicatedVertices.Count;
                    indexStart = newIndexes.Count;
                }

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
                    newIndexes.Add((ushort)idxPos);
                }
            }

            //create the final material/submesh
            SubMeshes.Add(
                new SubmeshDesc
                {
                    MaterialIndex = lastMat,
                    VerticesStart = vertStart,
                    VerticesCount = deduplicatedVertices.Count - vertStart,
                    IndexStart = indexStart,
                    IndexCount = newIndexes.Count - indexStart
                });

            //dump the new vertices, normals and UVs
            Vertices.AddRange(deduplicatedVertices.SelectMany(v => new[] { v.Item1.X, v.Item1.Y, v.Item1.Z }));
            Normals.AddRange(deduplicatedVertices.SelectMany(v => new[] { v.Item3.X, v.Item3.Y, v.Item3.Z }));
            UVs.AddRange(deduplicatedVertices.SelectMany(v => new[] { v.Item2.X, v.Item2.Y }));

            Indices.AddRange(newIndexes);
        }

        private void CreateVertexCloud(List<Tuple<Vector3, Vector2, Vector3, int>> newVertsWithUvsAndNormalsAndMaterial)
        {
            foreach (var matAndface in _sortedFaces)
            {
                int verticesBase = Vertices.Count;

                PrimFace.FaceData faceData = (PrimFace.FaceData) matAndface.Value.UserData;

                for (int i = 0; i < faceData.Indices.Length; i += 3)
                {
                    ushort a = (ushort) (faceData.Indices[i] + (verticesBase/3));
                    ushort b = (ushort) (faceData.Indices[i + 1] + (verticesBase/3));
                    ushort c = (ushort) (faceData.Indices[i + 2] + (verticesBase/3));

                    newVertsWithUvsAndNormalsAndMaterial.Add(new Tuple<Vector3, Vector2, Vector3, int>(
                        new Vector3(-faceData.Vertices[a*3], faceData.Vertices[a*3 + 1], faceData.Vertices[a*3 + 2]),
                        new Vector2(faceData.TexCoords[a*2], faceData.TexCoords[a*2 + 1]),
                        new Vector3(-faceData.Normals[a*3], faceData.Normals[a*3 + 1], faceData.Normals[a*3 + 2]),
                        matAndface.Key
                        ));

                    newVertsWithUvsAndNormalsAndMaterial.Add(new Tuple<Vector3, Vector2, Vector3, int>(
                        new Vector3(-faceData.Vertices[b*3], faceData.Vertices[b*3 + 1], faceData.Vertices[b*3 + 2]),
                        new Vector2(faceData.TexCoords[b*2], faceData.TexCoords[b*2 + 1]),
                        new Vector3(-faceData.Normals[b*3], faceData.Normals[b*3 + 1], faceData.Normals[b*3 + 2]),
                        matAndface.Key
                        ));

                    newVertsWithUvsAndNormalsAndMaterial.Add(new Tuple<Vector3, Vector2, Vector3, int>(
                        new Vector3(-faceData.Vertices[c*3], faceData.Vertices[c*3 + 1], faceData.Vertices[c*3 + 2]),
                        new Vector2(faceData.TexCoords[c*2], faceData.TexCoords[c*2 + 1]),
                        new Vector3(-faceData.Normals[c*3], faceData.Normals[c*3 + 1], faceData.Normals[c*3 + 2]),
                        matAndface.Key
                        ));
                }
            }
        }
    }
}
