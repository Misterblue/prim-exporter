using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib
{
    public class ExpLib
    {
        public static void Init()
        {
            ImportExport.ExportFormatterFactory.Init();
            ImportExport.PackagerFactory.Init();
        }

        public static void ShutDown()
        {
            GroupLoader.Instance.ShutDown();
        }
    }
}
