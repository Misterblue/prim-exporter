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
using System.Threading.Tasks;

namespace InWorldz.PrimExporter.ExpLib.ImportExport {
    public class GltfPackager : IPackager {

        public Package CreatePackage(ExportResult result, string baseDir, PackagerParams packagerParams) {
            Console.Out.WriteLine("GltfPackager: fbCnt={0}, objName={1}, cName={2}, texCnt={3}, bObjCnt={4}",
                result.FaceBytes.Count,
                result.ObjectName,
                result.CreatorName,
                result.TextureFiles.Count,
                result.BaseObjects.Count
                );
            Console.Out.WriteLine("GltfPackager: ccreteCnt={0}, instCnt={1}, submCnt={2}. texCnt={3}, primCnt={4}",
                result.Stats.ConcreteCount,
                result.Stats.InstanceCount,
                result.Stats.SubmeshCount,
                result.Stats.TextureCount,
                result.Stats.PrimCount
            );


            throw new NotImplementedException();
        }
    }
}
