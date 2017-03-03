using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate pre-flatbuffer multimaterial object
    /// </summary>
    internal class MultiMaterial
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> MaterialsList { get; set; }
    }
}
