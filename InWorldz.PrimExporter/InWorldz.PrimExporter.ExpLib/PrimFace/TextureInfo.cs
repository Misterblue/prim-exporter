using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib.PrimFace
{
    public class TextureInfo
    {
        public System.Drawing.Image Texture;
        public bool HasAlpha;
        public bool FullAlpha;
        public bool IsMask;
        public bool IsInvisible;
        public OpenMetaverse.UUID TextureID;
    }
}
