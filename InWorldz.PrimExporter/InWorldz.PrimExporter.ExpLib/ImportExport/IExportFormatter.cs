using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    public interface IExportFormatter
    {
        /// <summary>
        /// Export a single prim
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        ExportResult Export(PrimDisplayData data);

        /// <summary>
        /// Export multiple prims
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        ExportResult Export(GroupDisplayData datas);
    }
}
