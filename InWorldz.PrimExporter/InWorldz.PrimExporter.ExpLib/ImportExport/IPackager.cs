using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib.ImportExport
{
    /// <summary>
    /// Interface for packaging an export result into a result suitable
    /// for copying to a desintation
    /// </summary>
    interface IPackager
    {
        Package CreatePackage(ExportResult result, string baseDir);
    }
}
