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
            return _packagers[name];
        }
    }
}