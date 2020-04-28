using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatchZone.Core
{
    public static class PatchZoneCore
    {
        public static readonly string GameInstallationPath;
        public static readonly string PatchZoneInstallationPath;

        static PatchZoneCore()
        {
            PatchZoneInstallationPath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            GameInstallationPath = Path.GetFullPath(Path.Combine(PatchZoneInstallationPath, ".."));
        }
    }
}
