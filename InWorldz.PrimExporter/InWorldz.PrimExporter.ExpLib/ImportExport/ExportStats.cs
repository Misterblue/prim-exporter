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

        public void Combine(ExportStats other) {
            this.ConcreteCount += other.ConcreteCount;
            this.InstanceCount += other.InstanceCount;
            this.SubmeshCount += other.SubmeshCount;
            this.TextureCount += other.TextureCount;
            this.PrimCount += other.PrimCount;
            this.GroupsByPrimCount.AddRange(other.GroupsByPrimCount);
            this.GroupsBySubmeshCount.AddRange(other.GroupsBySubmeshCount);
        }
    }
}
