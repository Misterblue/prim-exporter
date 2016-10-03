using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using OpenMetaverse;
using ServiceStack.Text;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class BabylonJSONPackager : IPackager
    {
        public Package CreatePackage(ExportResult res, string baseDir)
        {
            string dirName = Path.Combine(baseDir, UUID.Random().ToString());
            Directory.CreateDirectory(dirName);
            File.WriteAllBytes(Path.Combine(dirName, "object.babylon"), res.FaceBytes[0]);
            
            foreach (var img in res.TextureFiles)
            {
                File.Move(img, Path.Combine(dirName, Path.GetFileName(img)));
            }

            return new Package { Path = dirName };
        }
    }
}