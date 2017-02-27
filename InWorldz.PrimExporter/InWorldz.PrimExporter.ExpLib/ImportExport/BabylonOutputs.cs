using OpenMetaverse;
using System.Collections.Generic;
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
