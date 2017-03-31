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

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public class ExportResult
    {
        public Tuple<byte[], int, int> FaceBlob; //data, start, length
        public List<byte[]> FaceBytes = new List<byte[]>();
        public List<string> TextureFiles = new List<string>();
        public List<PrimDisplayData> BaseObjects = new List<PrimDisplayData>();
        public string ObjectName;
        public string CreatorName;
        public ExportStats Stats = new ExportStats();
        public ulong Hash = 0;

        public void Combine(ExportResult other)
        {
            this.FaceBytes.AddRange(other.FaceBytes);
            this.TextureFiles.AddRange(other.TextureFiles);
            this.BaseObjects.AddRange(other.BaseObjects);
        }
    }
}
