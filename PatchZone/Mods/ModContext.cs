using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Core;
using PatchZone.Core.Mods;
using PatchZone.Core.Printers;
using PatchZone.Core.Utils;
using PatchZone.Hatch;
using PatchZone.Utils;

namespace PatchZone.Mods
{
    public class ModContext : IPatchZoneContext
    {
        public ModLog Log { get; }
        public ModManifest Manifest { get; }

        public string SourcePath { get; }
        public IPatchZoneMod Hatch { get; }

        public List<(Type Service, Type PatchImpl)> ServicePatches { get; } = new List<(Type, Type)>();

        public ModContext(ModInfo modInfo)
        {
            this.SourcePath = ModUtils.GetModDirectory(modInfo);

            var logPrefix = '[' + ModUtils.GetReadableIdentifier(modInfo) + "] ";

            this.Log = new ModLog
            {
                CurrentLogLevel = ModLog.LogLevel.Normal,
                Debug = new PrefixPrinter(GlobalLog.Debug, logPrefix),
                Error = new PrefixPrinter(GlobalLog.Error, logPrefix),
                Normal = new PrefixPrinter(GlobalLog.Default, logPrefix)
            };

            var manifestPath = ModUtils.GetManifestPath(this.SourcePath);
            this.Manifest = XML.Deserialize<ModManifest>(manifestPath);
            
            //TODO: Deserialize from manifest
            var dataSources = new List<IDataSource>();
            dataSources.Add(new ManagedDllSource());

            foreach(var dataSource in dataSources)
            {
                dataSource.Initialize(this);
            }

            var modHatchType = typeof(NullHatch);

            foreach(var dataSource in dataSources)
            foreach(var assembly in dataSource.ManagedAssemblies)
            foreach(var type in assembly.GetExportedTypes())
            {
                if(typeof(IPatchZoneMod).IsAssignableFrom(type))
                {
                    modHatchType = type;
                    break;
                }
            }

            this.Log.Log("Instantiating mod hatch " + modHatchType);
            this.Hatch = (IPatchZoneMod) Activator.CreateInstance(modHatchType);
            this.Hatch.Init(this);
        }

        public void RegisterServicePatch(Type service, Type patchImplementation)
        {
            this.ServicePatches.Add((service, patchImplementation));
        }

        class NullHatch : IPatchZoneMod
        {
            public void Init(IPatchZoneContext context)
            { }

            public void OnBeforeGameStart()
            { }
        }
    }
}
