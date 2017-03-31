using System.Collections.Generic;
using InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates;
using OpenMetaverse;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Outputs required to generate flatbuffer objects
    /// </summary>
    internal class BabylonFlatBufferOutputs
    {
        public List<MeshInstance> Instances { get; } = new List<MeshInstance>();

        public Dictionary<ulong, BabylonFlatBufferIntermediates.Material> Materials { get; } 
            = new Dictionary<ulong, BabylonFlatBufferIntermediates.Material>();

        public Dictionary<ulong, MultiMaterial> MultiMaterials { get; } 
            = new Dictionary<ulong, MultiMaterial>();

        public Dictionary<UUID, TrackedTexture> Textures { get; } = new Dictionary<UUID, TrackedTexture>();
        public List<string> TextureFiles { get; } = new List<string>();
    }
}
