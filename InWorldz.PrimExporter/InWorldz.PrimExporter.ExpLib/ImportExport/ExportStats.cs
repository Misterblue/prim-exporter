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
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Statistics relating to exported objects
    /// </summary>
    public class ExportStats
    {
        /// <summary>
        /// The number of concrete multi-meshes 
        /// </summary>
        public int ConcreteCount { get; set; }

        /// <summary>
        /// The number of multi-mesh instances
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// Count of all submeshes
        /// </summary>
        public int SubmeshCount { get; set; }

        /// <summary>
        /// Number of unique textures
        /// </summary>
        public int TextureCount { get; set; }

        /// <summary>
        /// The number of non-instanced primitives
        /// </summary>
        public int PrimCount { get; set; }

        /// <summary>
        /// A list of all groups and their prim counts
        /// </summary>
        public List<Tuple<string, int>> GroupsByPrimCount { get; set; } = new List<Tuple<string, int>>();

        /// <summary>
        /// A list of all groups and their submesh counts
        /// </summary>
        public List<Tuple<string, int>> GroupsBySubmeshCount { get; set; } = new List<Tuple<string, int>>();
    }
}
