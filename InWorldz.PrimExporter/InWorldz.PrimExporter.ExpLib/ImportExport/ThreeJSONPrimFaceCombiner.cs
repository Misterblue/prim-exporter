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

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class ThreeJSONPrimFaceCombiner
    {
        [Flags]
        private enum FaceDataFlag
        {
            HasNothing = 0,

            IsQuad = (1 << 0),
            HasMaterial = (1 << 1),
            HasFaceUV = (1 << 2),
            HasFaceVertexUV = (1 << 3),
            HasFaceNormal = (1 << 4),
            HasFaceVertexNormal = (1 << 5),
            HasFaceColor = (1 << 6),
            HasFaceVertexColor = (1 << 7)
        }

        public List<ushort> EncodedIndices = new List<ushort>();
        public List<float> Vertices = new List<float>();
        public List<float> Normals = new List<float>();
        public List<float> UVs = new List<float>();
        public List<OpenMetaverse.Primitive.TextureEntryFace> Materials = new List<OpenMetaverse.Primitive.TextureEntryFace>();
        public int TotalFaces = 0;

        public void CombineFace(OpenMetaverse.Rendering.Face face)
        {
            int verticesBase = Vertices.Count;

            PrimFace.FaceData faceData = (PrimFace.FaceData)face.UserData;

            //dump the vertices as they are
            Vertices.AddRange(faceData.Vertices);
            Normals.AddRange(faceData.Normals);
            UVs.AddRange(faceData.TexCoords);
                
            //dump a material for the entire VIEWER FACE
            Materials.Add(face.TextureFace);

            for (int i = 0; i < faceData.Indices.Length; i+=3)
            {
                ushort materialsIndex = (ushort)(Materials.Count - 1);

                ushort a = (ushort)(faceData.Indices[i] + (verticesBase / 3));
                ushort b = (ushort)(faceData.Indices[i + 1] + (verticesBase / 3));
                ushort c = (ushort)(faceData.Indices[i + 2] + (verticesBase / 3));

                EncodedIndices.AddRange(EncodeFace(materialsIndex, a, b, c));
            }

            TotalFaces += faceData.Indices.Length / 3;
        }

        private IEnumerable<ushort> EncodeFace(ushort materialsIndex, ushort a, ushort b, ushort c)
        {
            const FaceDataFlag flag = FaceDataFlag.HasMaterial | FaceDataFlag.HasFaceVertexUV | FaceDataFlag.HasFaceVertexNormal;

            List<ushort> encodedFace = new List<ushort>();
            encodedFace.Add((ushort)flag);
            encodedFace.Add(a);
            encodedFace.Add(b);
            encodedFace.Add(c);

            //Material for this face
            encodedFace.Add(materialsIndex);

            //UVs, one per vert
            encodedFace.Add((ushort)a);
            encodedFace.Add((ushort)b);
            encodedFace.Add((ushort)c);

            //Normals, one per vert
            encodedFace.Add((ushort)a);
            encodedFace.Add((ushort)b);
            encodedFace.Add((ushort)c);

            return encodedFace;
        }
    }
}
