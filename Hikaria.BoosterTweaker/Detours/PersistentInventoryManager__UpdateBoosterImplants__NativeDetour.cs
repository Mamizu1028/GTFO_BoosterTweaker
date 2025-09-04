using DropServer.BoosterImplants;
using Hikaria.BoosterTweaker.Managers;
using Hikaria.Core.Utility;
using Il2CppInterop.Runtime.Runtime;

namespace Hikaria.BoosterTweaker.Detours
{
    internal unsafe class PersistentInventoryManager__UpdateBoosterImplants__NativeDetour : EasyDetourBase<PersistentInventoryManager__UpdateBoosterImplants__NativeDetour.UpdateBoosterImplantsDel>
    {
        public delegate void UpdateBoosterImplantsDel(IntPtr instancePtr, IntPtr playerData, Il2CppMethodInfo* methodInfo);

        public override DetourDescriptor Descriptor => new()
        {
            Type = typeof(PersistentInventoryManager),
            MethodName = nameof(PersistentInventoryManager.UpdateBoosterImplants),
            ArgTypes = new Type[] { typeof(BoosterImplantPlayerData) },
            ReturnType = typeof(void),
            IsGeneric = false
        };

        public override UpdateBoosterImplantsDel DetourTo => Detour;

        private void Detour(IntPtr instancePtr, IntPtr playerData, Il2CppMethodInfo* methodInfo)
        {
            if (Features.PerfectBooster.Settings.EnableCustomPerfectBooster)
            {
                CustomPerfectBoosterImplantManager.ApplyCustomPerfectBoosterImplants();
                return;
            }
            else if (Features.CustomBooster.Settings.EnableCustomBooster)
            {
                CustomBoosterImplantManager.ApplyCustomBoosterImplants();
                return;
            }
            Original(instancePtr, playerData, methodInfo);
            if (Features.PerfectBooster.Settings.EnablePerfectBooster)
            {
                Features.PerfectBooster.ApplyPerfectBoosterImplants();
                return;
            }
        }
    }
}
