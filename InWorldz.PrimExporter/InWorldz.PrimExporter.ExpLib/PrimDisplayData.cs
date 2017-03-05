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
using System.Text;
using PrimMesher;
using OpenMetaverse;

namespace InWorldz.PrimExporter.ExpLib
{
    /// <summary>
    /// Data required to draw a primitive including the mesh and the texture/face data
    /// </summary>
    public class PrimDisplayData
    {
        public PrimDisplayData Parent;
        public OpenMetaverse.Rendering.FacetedMesh Mesh;
        public bool IsRootPrim;
        public Vector3 OffsetPosition;
        public Quaternion OffsetRotation;
        public Vector3 Scale;
        public ulong ShapeHash;
        public ulong MaterialHash;
        public int LinkNum;
    }
}
