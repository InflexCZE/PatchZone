using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatchZone.Core.Mods
{
    public static class ModUtils
    {
        public static readonly string ModsStorageRoot;
        public static readonly string ManifestName = "PatchZoneManifest.xml";

        static ModUtils()
        {
            ModsStorageRoot = Path.Combine(PatchZoneCore.PatchZoneInstallationPath, "Mods");
        }

        public static string GetModDirectory(ModInfo mod)
        {
            return Path.Combine(ModsStorageRoot, mod.Guid.ToString());
        }

        public static string GetModDirectory(ModManifest mod)
        {
            return Path.Combine(ModsStorageRoot, mod.Guid.ToString());
        }

        public static string GetManifestPath(string modDirectory)
        {
            return Path.Combine(modDirectory, ManifestName);
        }

        public static string GetReadableIdentifier(ModInfo mod)
        {
            return $"{mod.DisplayName} ({mod.Guid})";
        }

        public static ModInfo BuildModInfoFromManifest(ModManifest manifest, bool active = false)
        {
            return new ModInfo
            {
                Active = active,
                Guid = manifest.Guid,
                DisplayName = manifest.DisplayName
            };
        }
            
    }
}
