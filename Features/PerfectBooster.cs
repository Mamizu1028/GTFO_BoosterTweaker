using BoosterImplants;
using CellMenu;
using Clonesoft.Json;
using DropServer.BoosterImplants;
using GameData;
using Hikaria.BoosterTweaker.Managers;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using static Hikaria.BoosterTweaker.Managers.BoosterImplantTemplateManager;
using static Hikaria.BoosterTweaker.Managers.CustomPerfectBoosterImplantManager;

namespace Hikaria.BoosterTweaker.Features;

[EnableFeatureByDefault]
public class PerfectBooster : Feature
{
    public override string Name => "Perfect Booster";

    [FeatureConfig]
    public static PerfectBoosterSetting Settings { get; set; }

    public override bool RequiresRestart => true;

    public override Type[] LocalizationExternalTypes => new Type[] { typeof(BoosterImplantCategory), typeof(AgentModifier), typeof(BoosterCondition) };

    public class PerfectBoosterSetting
    {
        [FSDisplayName("快速刷取")]
        public bool EnableBoosterFarmer { get; set; }
        [RequiresRestart]
        [FSDisplayName("完美强化剂")]
        public bool EnablePerfectBooster { get; set; } = true;
        [FSDisplayName("禁用消耗")]
        public bool DisableBoosterConsume { get; set; } = true;
        [FSHeader("自定义强化剂")]
        [FSDisplayName("启用自定义强化剂")]
        public bool EnableCustomPerfectBooster { get => BoosterImplantTemplateManager.EnableCustomPerfectBooster; set => BoosterImplantTemplateManager.EnableCustomPerfectBooster = value; }
        [FSDisplayName("通过现有强化剂生成自定义强化剂")]
        public FButton CreateCustomPerfectBoosterFromInventory { get; set; } = new("生成", "生成自定义强化剂", CreateCustomPerfectBoosterImplantsFromInventory);
        [FSDisplayName("通过设置文件重载自定义强化剂")]
        public FButton LoadCustomPerfectBoosterFromSettings { get; set; } = new("重载", "重载自定义强化剂设置", CustomPerfectBoosterImplants.Load);
        [FSDisplayName("应用自定义强化剂")]
        public FButton ApplyCustomPerfectBoosters { get; set; } = new("应用", "应用自定义强化剂", ApplyCustomPerfectBoosterImplants);
        [JsonIgnore]
        [FSReadOnly]
        [FSDisplayName("编辑自定义强化剂")]
        public Dictionary<BoosterImplantCategory, CustomPerfectBoosterImplantEntryListEntry> CustomPerfectBoosterImplantsEntry
        {
            get
            {
                Dictionary<BoosterImplantCategory, CustomPerfectBoosterImplantEntryListEntry> result = new();
                for (int i = 0; i < 3; i++)
                {
                    var category = (BoosterImplantCategory)i;
                    var entries = new List<CustomPerfectBoosterImplantEntry>();
                    for (int j = 0; j < CustomPerfectBoosterImplants.Value[category].Count; j++)
                    {
                        entries.Add(new(CustomPerfectBoosterImplants.Value[category][j]));
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

    public class CustomPerfectBoosterImplantEntryListEntry
    {
        public CustomPerfectBoosterImplantEntryListEntry(List<CustomPerfectBoosterImplantEntry> entries)
        {
            Entries = entries;
        }

        [FSInline]
        [FSDisplayName("强化剂列表")]
        public List<CustomPerfectBoosterImplantEntry> Entries { get; set; } = new();
    }

    public class CustomPerfectBoosterImplantEntry
    {
        public CustomPerfectBoosterImplantEntry(CustomPerfectBoosterImplant implant)
        {
            Implant = implant;
            TemplateId = implant.TemplateId;
            var templates = BoosterImplantTemplates.FindAll(p => p.BoosterImplantID == implant.TemplateId);
            var template = templates[implant.TemplateIndex];
            if (template != null && template.BoosterImplantID != 0)
            {
                Templates.EffectsTemplates.Clear();
                for (int i = 0; i < template.EffectGroups.Count; i++)
                {
                    Templates.EffectsTemplates.Add(new(i, template.EffectGroups[i]));
                }
                Templates.ConditionsTemplates.Clear();
                for (int i = 0; i < template.ConditionGroups.Count; i++)
                {
                    Templates.ConditionsTemplates.Add(new(i, template.ConditionGroups[i]));
                }
                TemplateId = template.BoosterImplantID;
            }
        }

        private void UpdateTemplate()
        {
            var templates = BoosterImplantTemplates.FindAll(p => p.BoosterImplantID == TemplateId);
            if (TemplateIndex <= -1 || TemplateIndex >= templates.Count)
                return;
            var template = templates[TemplateIndex];
            if (template != null && template.BoosterImplantID != 0)
            {
                Templates.EffectsTemplates.Clear();
                for (int i = 0; i < template.EffectGroups.Count; i++)
                {
                    Templates.EffectsTemplates.Add(new(i, template.EffectGroups[i]));
                }
                Templates.ConditionsTemplates.Clear();
                for (int i = 0; i < template.ConditionGroups.Count; i++)
                {
                    Templates.ConditionsTemplates.Add(new(i, template.ConditionGroups[i]));
                }
                TemplateId = template.BoosterImplantID;
            }
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

        [FSDisplayName("模版索引")]
        public int TemplateIndex { get => Implant.TemplateIndex; set { Implant.TemplateIndex = value; UpdateTemplate(); } }

        [FSDisplayName("效果组索引")]
        public int EffectsGroupIndex { get => Implant.EffectGroupIndex; set => Implant.EffectGroupIndex = value; }

        [FSDisplayName("条件组索引")]
        public int ConditionsGroupIndex { get => Implant.ConditionGroupIndex; set => Implant.ConditionGroupIndex = value; }

        [FSDisplayName("状态")]
        public bool Enabled { get => Implant.Enabled; set => Implant.Enabled = value; }

        [JsonIgnore]
        [FSInline]
        [FSDisplayName("可选模板")]
        public CustomPerfectBoosterTemplateEntry Templates { get; set; } = new();

        [FSIgnore]
        private CustomPerfectBoosterImplant Implant { get; set; }
    }

    public class CustomPerfectBoosterTemplateEntry
    {
        [FSReadOnly]
        [FSDisplayName("可选效果模板")]
        public List<BoosterImplantEffectGroupPreferenceEntry> EffectsTemplates { get; set; } = new();

        [FSReadOnly]
        [FSDisplayName("可选条件模板")]
        public List<BoosterImplantConditionGroupPreferenceEntry> ConditionsTemplates { get; set; } = new();
    }

    public class BoosterImplantEffectGroupPreferenceEntry
    {
        public BoosterImplantEffectGroupPreferenceEntry(int index, List<BoosterImplantEffectTemplate> effectGroup)
        {
            Index = index;
            Effects.Clear();
            for (int i = 0; i < effectGroup.Count; i++)
            {
                var block = BoosterImplantEffectDataBlock.GetBlock(effectGroup[i].BoosterImplantEffect);
                if (block != null)
                {
                    Effects.Add(new(block));
                }
            }
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("效果组索引")]
        public int Index { get; set; }

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("效果列表")]
        public List<EffectEntry> Effects { get; set; } = new();

        public class EffectEntry
        {
            public EffectEntry(BoosterImplantEffectDataBlock block)
            {
                Effect = block.Effect;
            }

            [FSReadOnly]
            [FSDisplayName("效果")]
            public AgentModifier Effect { get; set; }
        }
    }

    public class BoosterImplantConditionGroupPreferenceEntry
    {
        public BoosterImplantConditionGroupPreferenceEntry(int index, List<uint> conditionGroup)
        {
            Index = index;
            Conditions.Clear();
            for (int i = 0; i < conditionGroup.Count; i++)
            {
                var block = BoosterImplantConditionDataBlock.GetBlock(conditionGroup[i]);
                if (block != null)
                {
                    Conditions.Add(new(block));
                }
            }
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("条件组索引")]
        public int Index { get; set; }

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("条件列表")]
        public List<ConditionEntry> Conditions { get; set; } = new();
        public class ConditionEntry
        {
            public ConditionEntry(BoosterImplantConditionDataBlock block)
            {
                Condition = block.Condition;
            }

            [FSReadOnly]
            [FSDisplayName("条件")]
            public BoosterCondition Condition { get; set; }
        }
    }

    [ArchivePatch(typeof(LocalizationManager), nameof(LocalizationManager.Setup))]
    private class LocalizationManager__Setup__Patch
    {
        private static void Postfix()
        {
            CustomPerfectBoosterImplants.Load();
        }
    }

    [ArchivePatch(typeof(CM_PageLoadout), nameof(CM_PageLoadout.ProcessBoosterImplantEvents))]
    private class CM_PageLoadout__ProcessBoosterImplantEvents__Patch
    {
        private static bool Prefix()
        {
            if (Settings.EnableCustomPerfectBooster)
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
            if (Settings.DisableBoosterConsume || Settings.EnablePerfectBooster)
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
            return !(Settings.DisableBoosterConsume || Settings.EnableCustomPerfectBooster);
        }
    }

    [ArchivePatch(typeof(BoosterUtils), nameof(BoosterUtils.BoosterCurrencyFromHeatAndArtifactCount))]
    private class BoosterUtils__BoosterCurrencyFromHeatAndArtifactCount__Patch
    {
        private static void Postfix(ref int __result)
        {
            if (Settings.EnableCustomPerfectBooster)
            {
                __result = 0;
                return;
            }
            if (Settings.EnableBoosterFarmer)
            {
                __result = 100000;
                return;
            }
        }
    }

    public override void OnGameDataInitialized()
    {
        LoadTemplateData();
    }

    public static void ApplyPerfectBoosterImplants()
    {
        for (int i = 0; i < PersistentInventoryManager.Current.m_boosterImplantInventory.Categories.Count; i++)
        {
            var category = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i];
            var inventory = category.Inventory;
            for (int j = 0; j < inventory.Count; j++)
            {
                var boosterImplant = inventory[j].Implant;
                if (TryGetBoosterImplantTemplate(boosterImplant, out _, out var effectGroup, out var conditions, out _, out _))
                {
                    ApplyPerfectBoosterFromTemplate(boosterImplant, effectGroup, conditions);
                }
            }
        }
    }
}
