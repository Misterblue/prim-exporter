using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Lists of items that must be tracked for a babylon file
    /// </summary>
    internal class BabylonOutputs
    {
        public Dictionary<ulong, string> Materials { get; } = new Dictionary<ulong, string>();
        public Dictionary<UUID, TrackedTexture> Textures { get; } = new Dictionary<UUID, TrackedTexture>();
        public Dictionary<ulong, string> MultiMaterials { get; } = new Dictionary<ulong, string>();
        public List<string> TextureFiles { get; } = new List<string>();
    }
}
