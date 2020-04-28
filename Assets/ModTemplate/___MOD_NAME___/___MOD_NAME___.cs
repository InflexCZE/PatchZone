using System;
using PatchZone.Hatch;
using PatchZone.Hatch.Utils;
using ___MOD_NAME___.Services;

using ECS;
using Service.Achievement;
using Service.Building;
using Service.Localization;
using Service.Street;
using Service.UserWorldTasks;

namespace ___MOD_NAME___
{
    public class ___MOD_NAME___ : Singleton<___MOD_NAME___>, IPatchZoneMod
    {
        public IPatchZoneContext Context { get; private set; }

        public void Init(IPatchZoneContext context)
        {
            this.Context = context;
        }

        public void OnBeforeGameStart()
        {
            this.Context.RegisterProxyService<ILocalizationService, LocalizationService>();
        }
    }
}
