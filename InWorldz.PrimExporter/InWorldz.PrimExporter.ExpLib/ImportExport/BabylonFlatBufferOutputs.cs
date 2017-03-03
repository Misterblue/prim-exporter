using System.Collections.Generic;
using InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates;
using OpenMetaverse;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    internal class BabylonFlatBufferOutputs
    {
        public List<MeshInstance> Instances { get; } = new List<MeshInstance>();
        public Dictionary<ulong, object> Materials { get; } = new Dictionary<ulong, object>();
        public Dictionary<ulong, object> MultiMaterials { get; } = new Dictionary<ulong, object>();
        public Dictionary<UUID, TrackedTexture> Textures { get; } = new Dictionary<UUID, TrackedTexture>();
        public List<string> TextureFiles { get; } = new List<string>();
    }
}
