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
using OpenMetaverse;
using System.Collections.Generic;
using FlatBuffers;
using InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBuffers;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Lists of items that must be tracked for a babylon file (JSON and Flatbuffer)
    /// </summary>
    internal class BabylonOutputs
    {
        public Dictionary<ulong, object> Materials { get; } = new Dictionary<ulong, object>();
        public Dictionary<ulong, object> MultiMaterials { get; } = new Dictionary<ulong, object>();
        public Dictionary<UUID, TrackedTexture> Textures { get; } = new Dictionary<UUID, TrackedTexture>();
        public List<string> TextureFiles { get; } = new List<string>();
    }
}
