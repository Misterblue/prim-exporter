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
using System.Collections.Generic;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Not really a factory. Returns registered instances of packagers by name
    /// </summary>
    public class PackagerFactory
    {
        private static readonly PackagerFactory instance = new PackagerFactory();

        private readonly Dictionary<string, IPackager> _packagers = new Dictionary<string, IPackager>();

        static PackagerFactory()
        {
        }

        public static void Init()
        {
            instance.Register(new ThreeJSONPackager(), "ThreeJSONPackager");
            instance.Register(new BabylonJSONPackager(), "BabylonJSONPackager");
        }

        private PackagerFactory()
        {
        }

        public static PackagerFactory Instance => instance;

        public void Register(IPackager packager, string name)
        {
            _packagers.Add(name, packager);
        }

        public IPackager Get(string name)
        {
            IPackager foundPackager;

            if (! _packagers.TryGetValue(name, out foundPackager))
            {
                throw new KeyNotFoundException($"Packager {name} was not found");
            }

            return foundPackager;
        }
    }
}
