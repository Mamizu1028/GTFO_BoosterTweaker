using Hikaria.BoosterTweaker.Detours;
using Hikaria.Core;
using Hikaria.Core.Utility;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.Localization;
using TheArchive.Interfaces;

namespace Hikaria.BoosterTweaker;

[ArchiveDependency(CoreGlobal.GUID)]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public void Init()
    {
        Logs.Setup(Logger);
        EasyDetour.CreateAndApply<PersistentInventoryManager__UpdateBoosterImplants__NativeDetour>(out s_detour);
    }

    private static IEasyDetour s_detour;

    public ILocalizationService LocalizationService { get; set; }
    public IArchiveLogger Logger { get; set; }
}

