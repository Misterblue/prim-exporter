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
