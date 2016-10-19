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
        public Package CreatePackage(ExportResult res, string baseDir, PackagerParams packagerParams)
        {
            string dirName = packagerParams.Direct ? baseDir : Path.Combine(baseDir, UUID.Random().ToString());
            Directory.CreateDirectory(dirName);

            //write the object
            File.WriteAllBytes(Path.Combine(dirName, "object.babylon"), res.FaceBytes[0]);
            
            //write the manifest
            var manifest = new
            {
                version = 1,
                enableSceneOffline = true,
                enableTexturesOffline = true
            };

            using (FileStream stream = File.OpenWrite(Path.Combine(dirName, "object.babylon.manifest")))
            {
                JsonSerializer.SerializeToStream(manifest, stream);
            }

            //textures..
            foreach (var img in res.TextureFiles)
            {
                var destImgPath = Path.Combine(dirName, Path.GetFileName(img));
                if (File.Exists(destImgPath))
                {
                    File.Delete(destImgPath);
                }

                File.Move(img, destImgPath);
            }

            return new Package { Path = dirName };
        }
    }
}