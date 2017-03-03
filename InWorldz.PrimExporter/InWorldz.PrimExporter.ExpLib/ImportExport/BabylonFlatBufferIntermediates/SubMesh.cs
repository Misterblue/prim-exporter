using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate pre-flatbuffer submesh object
    /// </summary>
    internal class SubMesh
    {
        public int MaterialIndex { get; set; }
        public int VerticesStart { get; set; }
        public int VerticesCount { get; set; }
        public int IndexStart { get; set; }
        public int IndexCount { get; set; }
    }
}
