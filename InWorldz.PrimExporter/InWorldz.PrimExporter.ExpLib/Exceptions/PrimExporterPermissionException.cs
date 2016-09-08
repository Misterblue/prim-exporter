using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InWorldz.PrimExporter.ExpLib.Exceptions
{
    public class PrimExporterPermissionException : Exception
    {
        public PrimExporterPermissionException()
        {
        }

        public PrimExporterPermissionException(string message)
            : base(message)
        {

        }

        public PrimExporterPermissionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
