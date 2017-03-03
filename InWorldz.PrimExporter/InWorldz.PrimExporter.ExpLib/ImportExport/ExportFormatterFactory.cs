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
    /// <summary>
    /// Not really a factory. Returns registered instances of exporters by name
    /// </summary>
    public class ExportFormatterFactory
    {
        private static readonly ExportFormatterFactory instance = new ExportFormatterFactory();

        private Dictionary<string, IExportFormatter> _formatters = new Dictionary<string, IExportFormatter>();

        static ExportFormatterFactory()
        {
        }

        public static void Init()
        {
            instance.Register(new ThreeJSONFormatter(), "ThreeJSONFormatter");
            instance.Register(new BabylonJSONFormatter(), "BabylonJSONFormatter");
        }

        private ExportFormatterFactory()
        {
        }

        public static ExportFormatterFactory Instance
        {
            get
            {
                return instance;
            }
        }

        public void Register(IExportFormatter formatter, string name)
        {
            _formatters.Add(name, formatter);
        }

        public IExportFormatter Get(string name)
        {
            return _formatters[name];
        }
    }
}
