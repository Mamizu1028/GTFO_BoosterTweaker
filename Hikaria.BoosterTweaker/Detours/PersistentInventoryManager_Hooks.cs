using DropServer.BoosterImplants;
using Hikaria.BoosterTweaker.Managers;
using Hikaria.Core.Detour;
using Il2CppInterop.Runtime.Runtime;

namespace Hikaria.BoosterTweaker.Detours;

internal static partial class PersistentInventoryManager_Hooks
{
    [NativeDetour(typeof(PersistentInventoryManager), nameof(PersistentInventoryManager.UpdateBoosterImplants))]
    private unsafe static void UpdateBoosterImplants_Hook(IntPtr self, BoosterImplantPlayerData* playerData, Il2CppMethodInfo* methodInfo)
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
        UpdateBoosterImplants_Original(self, playerData, methodInfo);
        if (Features.PerfectBooster.Settings.EnablePerfectBooster)
        {
            Features.PerfectBooster.ApplyPerfectBoosterImplants();
            return;
        }
    }
}
