using BoosterImplants;
using CellMenu;
using Clonesoft.Json;
using DropServer.BoosterImplants;
using Hikaria.BoosterTweaker.Managers;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.Localization;
using static Hikaria.BoosterTweaker.Managers.CustomBoosterImplantManager;
using static Hikaria.BoosterTweaker.Managers.CustomBoosterImplantManager.CustomBoosterImplant;

namespace Hikaria.BoosterTweaker.Features;

[EnableFeatureByDefault]
[HideInModSettings]
public class CustomBooster : Feature
{
    public override string Name => "Custom Booster";

    [FeatureConfig]
    public static CustomBoosterSetting Settings { get; set; }

    public override bool RequiresRestart => true;

    public override Type[] LocalizationExternalTypes => new Type[] { typeof(BoosterImplantCategory), typeof(AgentModifier), typeof(BoosterCondition) };

    public class CustomBoosterSetting
    {
        [FSDisplayName("正面效果倍率")]
        public float BoosterPositiveEffectMultiplier { get => BoosterImplantTemplateManager.BoosterPositiveEffectMultiplier; set => BoosterImplantTemplateManager.BoosterPositiveEffectMultiplier = value; }

        [FSDisplayName("禁用条件")]
        public bool DisableBoosterConditions { get => BoosterImplantTemplateManager.DisableBoosterConditions; set => BoosterImplantTemplateManager.DisableBoosterConditions = value; }

        [FSDisplayName("禁用负面效果")]
        public bool DisableBoosterNegativeEffects { get => BoosterImplantTemplateManager.DisableBoosterNegativeEffects; set => BoosterImplantTemplateManager.DisableBoosterNegativeEffects = value; }

        [FSDisplayName("强化剂自定义")]
        [FSDescription("自定义将导致完美强化剂与模板首选项以及其他作弊选项失效")]
        public bool EnableCustomBooster { get => BoosterImplantTemplateManager.EnableCustomBooster; set => BoosterImplantTemplateManager.EnableCustomBooster = value; }

        [FSDisplayName("通过现有强化剂生成自定义强化剂")]
        public FButton CreateCustomBoosterFromInventory { get; set; } = new("生成", "生成自定义强化剂", CreateCustomBoosterImplantsFromInventory);

        [FSDisplayName("通过设置文件重载自定义强化剂")]
        public FButton LoadCustomBoosterFromSettings { get; set; } = new("重载", "重载自定义强化剂设置", CustomBoosterImplants.Load);

        [FSDisplayName("应用自定义强化剂")]
        public FButton ApplyCustomBoosters { get; set; } = new("应用", "应用自定义强化剂", ApplyCustomBoosterImplants);

        [JsonIgnore]
        [FSReadOnly]
        [FSDisplayName("编辑自定义强化剂")]
        public Dictionary<BoosterImplantCategory, CustomBoosterImplantEntryListEntry> CustomBoosterImplantsEntry
        {
            get
            {
                Dictionary<BoosterImplantCategory, CustomBoosterImplantEntryListEntry> result = new();
                for (int i = 0; i < 3; i++)
                {
                    var category = (BoosterImplantCategory)i;
                    var entries = new List<CustomBoosterImplantEntry>();
                    for (int j = 0; j < CustomBoosterImplants.Value[category].Count; j++)
                    {
                        entries.Add(new(CustomBoosterImplants.Value[category][j]));
                    }
                    result[category] = new(entries);
                }
                return result;
            }
            set
            {
            }
        }
    }

    public class CustomBoosterImplantEntryListEntry
    {
        public CustomBoosterImplantEntryListEntry(List<CustomBoosterImplantEntry> entries)
        {
            Entries = entries;
        }

        [FSInline]
        [FSDisplayName("强化剂列表")]
        public List<CustomBoosterImplantEntry> Entries { get; set; } = new();
    }

    public class CustomBoosterImplantEntry
    {
        public CustomBoosterImplantEntry(CustomBoosterImplant implant)
        {
            Implant = implant;
            Modifier = new(implant);
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("名称")]
        public string Name { get => Implant.Name; set { } }
        [FSReadOnly]
        [FSDisplayName("类别")]
        public BoosterImplantCategory Category { get => Implant.Category; set { } }
        [FSReadOnly]
        [FSDisplayName("ID")]
        public uint TemplateId { get => Implant.TemplateId; set { } }

        [FSDisplayName("效果列表")]
        public List<CustomBoosterImplantEffectEntry> Effects
        {
            get
            {
                List<CustomBoosterImplantEffectEntry> result = new();
                for (int i = 0; i < Implant.Effects.Count; i++)
                {
                    result.Add(new(Implant.Effects[i]));
                }
                return result;
            }
            set
            {
            }
        }

        [FSDisplayName("条件列表")]
        public List<CustomBoosterImplantConditionEntry> Conditions
        {
            get
            {
                List<CustomBoosterImplantConditionEntry> conditions = new();
                for (int i = 0; i < Implant.Conditions.Count; i++)
                {
                    conditions.Add(new(i, Implant));
                }
                return conditions;
            }
            set
            {
            }
        }

        [FSDisplayName("状态")]
        public bool Enabled { get => Implant.Enabled; set => Implant.Enabled = value; }

        [FSDisplayName("修改")]
        public BoosterImplantModifier Modifier { get; set; }

        [FSIgnore]
        private CustomBoosterImplant Implant { get; set; }
    }

    [Localized]
    public enum ModifyType
    {
        Add,
        Remove,
        Modify
    }

    [Localized]
    public enum ModifyTarget
    {
        Effect,
        Condition
    }

    public class BoosterImplantModifier
    {
        public BoosterImplantModifier(CustomBoosterImplant implant)
        {
            Implant = implant;
            Modify = new FButton("应用", "应用修改", DoModify);
        }

        [FSDisplayName("ID")]
        public uint Id { get; set; } = 0;

        [FSDisplayName("值")]
        public float Value { get; set; } = 1f;

        [FSDisplayName("修改模式")]
        public ModifyType type { get; set; } = ModifyType.Modify;

        [FSDisplayName("修改对象")]
        public ModifyTarget target { get; set; } = ModifyTarget.Effect;

        [FSDisplayName("应用修改")]
        public FButton Modify { get; set; }

        private void DoModify()
        {
            var effects = Implant.Effects;
            var conditions = Implant.Conditions;
            switch (type)
            {
                case ModifyType.Modify:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index == -1) return;
                        effects[index].Value = Value;
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index == -1) return;
                        conditions[index] = Id;
                    }
                    break;
                case ModifyType.Add:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index != -1) return;
                        effects.Add(new() { Id = Id, Value = Value });
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index != -1) return;
                        conditions.Add(Id);
                    }
                    break;
                case ModifyType.Remove:
                    if (target == ModifyTarget.Effect)
                    {
                        var index = effects.FindIndex(p => p.Id == Id);
                        if (index == -1) return;
                        effects.RemoveAt(index);
                    }
                    else
                    {
                        var index = conditions.FindIndex(p => p == Id);
                        if (index == -1) return;
                        conditions.RemoveAt(index);
                    }
                    break;
            }
        }

        private CustomBoosterImplant Implant { get; set; }
    }

    public class CustomBoosterImplantEffectEntry
    {
        public CustomBoosterImplantEffectEntry(Effect effect)
        {
            Effect = effect;
        }

        [FSSeparator]
        [FSDisplayName("效果ID")]
        public uint Id { get => Effect.Id; set => Effect.Id = value; }
        [FSDisplayName("效果数值")]
        public float Value { get => Effect.Value; set => Effect.Value = value; }

        [FSIgnore]
        private Effect Effect { get; set; }
    }

    public class CustomBoosterImplantConditionEntry
    {
        public CustomBoosterImplantConditionEntry(int index, CustomBoosterImplant implant)
        {
            Index = index;
            Implant = implant;
        }

        [FSIgnore]
        public int Index { get; set; }

        [FSSeparator]
        [FSDisplayName("条件")]
        public uint Condition { get => Implant.Conditions[Index]; set => Implant.Conditions[Index] = value; }

        [FSIgnore]
        private CustomBoosterImplant Implant { get; set; }
    }


    //[ArchivePatch(typeof(PersistentInventoryManager), nameof(PersistentInventoryManager.UpdateBoosterImplants))]
    private class PersistentInventoryManager__UpdateBoosterImplants__Patch
    {
        private static bool Prefix(PersistentInventoryManager __instance)
        {
            if (Settings.EnableCustomBooster)
            {
                __instance.m_boosterImplantDirtyState = PersistentInventoryManager.BoosterImplantDirtyState.UpToDate;
                ApplyCustomBoosterImplants();
                return false;
            }
            return true;
        }
    }

    [ArchivePatch(typeof(CM_PageLoadout), nameof(CM_PageLoadout.ProcessBoosterImplantEvents))]
    private class CM_PageLoadout__ProcessBoosterImplantEvents__Patch
    {
        private static bool Prefix()
        {
            if (Settings.EnableCustomBooster)
            {
                return false;
            }
            return true;
        }
    }

    [ArchivePatch(typeof(DropServerManager), nameof(DropServerManager.NewGameSession))]
    internal static class DropServerManager__NewGameSession__Patch
    {
        public static void Prefix(ref uint[] boosterIds)
        {
            if (Settings.EnableCustomBooster)
            {
                boosterIds = null;
            }
        }
    }

    [ArchivePatch(typeof(DropServerGameSession), nameof(DropServerGameSession.ConsumeBoosters))]
    private class DropServerGameSession__ConsumeBoosters__Patch
    {
        private static bool Prefix()
        {
            return !Settings.EnableCustomBooster;
        }
    }

    [ArchivePatch(typeof(BoosterUtils), nameof(BoosterUtils.BoosterCurrencyFromHeatAndArtifactCount))]
    private class BoosterUtils__BoosterCurrencyFromHeatAndArtifactCount__Patch
    {
        private static void Postfix(ref int __result)
        {
            if (Settings.EnableCustomBooster)
            {
                __result = 0;
                return;
            }
        }
    }
}
