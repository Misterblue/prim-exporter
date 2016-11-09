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
    }
}
