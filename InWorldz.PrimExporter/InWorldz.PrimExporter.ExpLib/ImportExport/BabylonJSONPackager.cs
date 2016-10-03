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

            using (var outFile = File.CreateText(Path.Combine(dirName, "object.babylon")))
            {
                outFile.WriteLine("{");
            }


            List<string> objectFiles = new List<string>();

            foreach (var bytes in res.FaceBytes)
            {
                string filePath = Path.Combine(dirName, UUID.Random().ToString() + ".js");
                File.WriteAllBytes(filePath, bytes);

                objectFiles.Add(Path.GetFileName(filePath));
            }

            foreach (var img in res.TextureFiles)
            {
                File.Move(img, Path.Combine(dirName, Path.GetFileName(img)));
            }

            List<object> offsetList = new List<object>();

            foreach (var dispInfo in res.BaseObjects)
            {
                offsetList.Add(new
                {
                    positionOffset = new float[] { dispInfo.OffsetPosition.X, dispInfo.OffsetPosition.Y, dispInfo.OffsetPosition.Z },
                    rotationOffset = new float[] { dispInfo.OffsetRotation.X, dispInfo.OffsetRotation.Y, dispInfo.OffsetRotation.Z, dispInfo.OffsetRotation.W },
                    scale = new float[] { dispInfo.Scale.X, dispInfo.Scale.Y, dispInfo.Scale.Z },
                    isRoot = dispInfo.IsRootPrim
                });
            }


            var summaryFile = new
            {
                objectName = res.ObjectName,
                creatorName = res.CreatorName,
                objectFiles = objectFiles,
                objectOffsets = offsetList.ToArray()
            };

            string summary = JsonSerializer.SerializeToString(summaryFile);
            byte[] summaryBytes = Encoding.UTF8.GetBytes(summary);
            File.WriteAllBytes(Path.Combine(dirName, "summary.js"), summaryBytes);

            return new Package { Path = dirName };
        }
    }
}