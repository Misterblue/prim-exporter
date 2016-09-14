using System.Collections.Generic;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Not really a factory. Returns registered instances of packagers by name
    /// </summary>
    public class PackagerFactory
    {
        private static readonly PackagerFactory instance = new PackagerFactory();

        private readonly Dictionary<string, IExportFormatter> _formatters = new Dictionary<string, IExportFormatter>();

        static PackagerFactory()
        {
        }

        public static void Init()
        {
            instance.Register(new ThreeJSONFormatter(), "ThreeJSONPackager");
            instance.Register(new ThreeJSONFormatter(), "BabylonJSONPackager");
        }

        private PackagerFactory()
        {
        }

        public static PackagerFactory Instance => instance;

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