using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace PatchZone.Patcher
{
    static partial class ServicePatcher
    {
        private static Type MethodPatcher = typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher");
        private static MethodInfo CreateReplacement = MethodPatcher.GetMethod("CreateReplacement", BindingFlags.Instance | BindingFlags.NonPublic);

        public static MethodInfo CreateDuplicate(MethodBase target)
        {
            var empty = new List<MethodInfo>();
            var methodPatcher = Activator.CreateInstance(MethodPatcher, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { target, null, empty, empty, empty, empty, false }, null, null);

            Dictionary<int, CodeInstruction> finalInstructions = null;
            return (MethodInfo) CreateReplacement.Invoke(methodPatcher, new[] { finalInstructions });
        }

        public static void RedirectMethod(MethodBase target, MethodBase redirectionTarget)
        {
            var error = Memory.DetourMethod(target, redirectionTarget);

            if(string.IsNullOrEmpty(error) == false)
            {
                throw new Exception(error);
            }
        }
    }
}
