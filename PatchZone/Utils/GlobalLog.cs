using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Core.Printers;
using PatchZone.Hatch.Utils;

namespace PatchZone.Utils
{
    class GlobalLog
    {
        public static IPrinter Error { get; }
        public static IPrinter Debug { get; }
        public static IPrinter Default { get; }

        static GlobalLog()
        {
            var defaultFile = new FilePrinter("PatchZone.log");
            var debugFile = new FilePrinter("PatchZone_Debug.log");
            var errorFile = new FilePrinter("PatchZone_Error.log")
            {
                AutoCommit = true
            };

            Debug = debugFile;
            Default = CompoundPrinter.Make(defaultFile, debugFile);
            Error = CompoundPrinter.Make
            (
                errorFile,
                new PrefixPrinter(debugFile, "Error: "),
                new PrefixPrinter(defaultFile, "Error: ")
            );

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            CloseLog(Error);
            CloseLog(Debug);
            CloseLog(Default);

            void CloseLog(IPrinter printer)
            {
                printer.Print("Log closed");
                printer.PrintNewLine();

                if (printer is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }
    }
}
