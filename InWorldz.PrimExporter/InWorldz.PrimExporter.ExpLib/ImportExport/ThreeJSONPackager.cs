// Copyright 2016 InWorldz Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using System.IO;
using ServiceStack.Text;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class ThreeJSONPackager : IPackager
    {
        public Package CreatePackage(ExportResult res, string baseDir, PackagerParams packagerParams)
        {
            string dirName = packagerParams.Direct ? baseDir : Path.Combine(baseDir, UUID.Random().ToString());
            Directory.CreateDirectory(dirName);

            List<string> objectFiles = new List<string>();

            foreach (var bytes in res.FaceBytes)
            {
                string filePath = Path.Combine(dirName, UUID.Random() + ".js");
                File.WriteAllBytes(filePath, bytes);

                objectFiles.Add(Path.GetFileName(filePath));
            }

            foreach (var img in res.TextureFiles)
            {
                var destImgPath = Path.Combine(dirName, Path.GetFileName(img));
                if (File.Exists(destImgPath))
                {
                    File.Delete(destImgPath);
                }

                File.Move(img, destImgPath);
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
