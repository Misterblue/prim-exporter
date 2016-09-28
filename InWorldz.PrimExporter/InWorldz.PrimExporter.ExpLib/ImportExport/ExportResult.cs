using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class ExportResult
    {
        public List<byte[]> FaceBytes = new List<byte[]>();
        public List<string> TextureFiles = new List<string>();
        public List<PrimDisplayData> BaseObjects = new List<PrimDisplayData>();
        public string ObjectName;
        public string CreatorName;
        public string Id;

        public void Combine(ExportResult other)
        {
            this.FaceBytes.AddRange(other.FaceBytes);
            this.TextureFiles.AddRange(other.TextureFiles);
            this.BaseObjects.AddRange(other.BaseObjects);
        }
    }
}
