using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using PatchZone.Core;
using PatchZone.Hatch.Utils;
using PatchZone.Mods;
using PatchZone.Patcher;
using PatchZone.Utils;

namespace PatchZone
{
    class PatchZone : Singleton<PatchZone>
    {
        public Harmony Harmony { get; }
        public Config Config { get; private set; }
        public List<ModContext> LoadedMods { get; } = new List<ModContext>();

        public void LoadMods()
        {
            var patchZone = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var modsBasePath = Path.Combine(patchZone, "Mods");

            foreach(var modInfo in this.Config.KnownMods)
            {
                if(modInfo.Active == false)
                    continue;

                var modPath = Path.Combine(modsBasePath, modInfo.Guid.ToString());
                var modContext = new ModContext(modPath);
                this.LoadedMods.Add(modContext);
            }
        }
        
        private bool GameStarted;
        public void OnBeforeGameStart()
        {
            if(this.GameStarted)
                return;

            this.GameStarted = true;

            foreach(var modContext in this.LoadedMods)
            {
                modContext.Hatch.OnBeforeGameStart();
            }

            InstallServices();
        }

        private void InstallServices()
        {
            try
            {
                var vanillaServices = global::Kernel.Instance.Container;
                var servicePatchStack = new Dictionary<Type, List<object>>();

                foreach (var mod in this.LoadedMods)
                foreach (var (service, patch) in mod.ServicePatches)
                {
                    var baseCascade = GetServicePatchStack(service);
                    var proxyService = ServicePatcher.InstantiatePatch(patch, baseCascade);
                    baseCascade.Insert(0, proxyService);
                }

                List<object> GetServicePatchStack(Type key)
                {
                    if (servicePatchStack.TryGetValue(key, out var baseCascade) == false)
                    {
                        baseCascade = new List<object>();

                        var vanilla = vanillaServices.Resolve(key);
                        if (vanilla == null)
                        {
                            throw new Exception("Vanilla service not found: " + key);
                        }

                        baseCascade.Add(vanilla);
                        servicePatchStack.Add(key, baseCascade);
                    }

                    return baseCascade;
                }
            }
            catch (Exception e)
            {
                GlobalLog.Error.PrintLine("Error while installing mod services");
                GlobalLog.Error.PrintLine(e.ToString());
                throw;
            }
        }


        public static void Start()
        {
            bool WaitForDebugger = false;

            while (WaitForDebugger && Debugger.IsAttached == false)
            {
                GlobalLog.Error.PrintLine("Awaiting debugger...");
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            //Note: Saves instance to Singleton
            var instance = new PatchZone();
        }

        public PatchZone()
        {
            TryConfigPath(Config.CONFIG_PATH);
            TryConfigPath(Path.Combine("PatchZone", Config.CONFIG_PATH));

            if(ReferenceEquals(this.Config, null))
            {
                this.Config = Config.Load();
            }

            this.Harmony = new Harmony("PatchZone");

            void TryConfigPath(string path)
            {
                if(ReferenceEquals(this.Config, null) == false)
                    return;

                if(File.Exists(path))
                {
                    this.Config = Config.Load(path);
                }
            }
        }
    }
}
