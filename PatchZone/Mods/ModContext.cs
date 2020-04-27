using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatchZone.Core;
using PatchZone.Core.Mods;
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

        public ModContext(string path)
        {
            this.SourcePath = path;

            this.Log = new ModLog
            {
                Debug = GlobalLog.Debug,
                Error = GlobalLog.Error,
                Normal = GlobalLog.Default,
                CurrentLogLevel = ModLog.LogLevel.Normal
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

            foreach(var dataSource in dataSources)
            foreach(var assembly in dataSource.ManagedAssemblies)
            foreach(var type in assembly.GetExportedTypes())
            {
                if(typeof(IPatchZoneMod).IsAssignableFrom(type))
                {
                    this.Hatch = (IPatchZoneMod) Activator.CreateInstance(type);
                    break;
                }
            }

            if(ReferenceEquals(this.Hatch, null))
            {
                this.Hatch = new NullHatch();
            }

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
