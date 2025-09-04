using BoosterImplants;
using Clonesoft.Json;
using Clonesoft.Json.Linq;
using GameData;
using Localization;
using TheArchive.Core.ModulesAPI;
using TheArchive.Utilities;

namespace Hikaria.BoosterTweaker.Managers;

public static class BoosterImplantTemplateManager
{
    public static float BoosterPositiveEffectMultiplier { get; set; } = 1f;
    public static bool DisableBoosterConditions { get; set; } = false;
    public static bool DisableBoosterNegativeEffects { get; set; } = false;
    public static bool EnableCustomPerfectBooster { get; set; } = false;
    public static bool EnableCustomBooster { get; set; } = false;

    public static CustomSetting<Dictionary<BoosterImplantCategory, List<bBoosterImplantTemplate>>> BoosterImplantTemplatesLookup { get; set; } = new("BoosterImplantTemplatesLookup", new()
    {
        { BoosterImplantCategory.Muted, new() },
        { BoosterImplantCategory.Bold, new() },
        { BoosterImplantCategory.Aggressive, new() }
    }, null, LoadingTime.None, false);

    public class bBoosterImplantTemplate
    {
        public bBoosterImplantTemplate(BoosterImplantTemplate template)
        {
            BoosterImplantID = template.BoosterImplantID;
            ImplantCategory = template.ImplantCategory;
            TemplateIndex = BoosterImplantTemplatesLookup.Value[template.ImplantCategory].Count(p => p.BoosterImplantID == template.BoosterImplantID);
            int index = 0;
            foreach (var group in template.EffectGroups)
            {
                var list = new List<bBoosterImplantEffectTemplate>();
                foreach (var effectTemplate in group)
                {
                    list.Add(new(effectTemplate));
                }
                EffectGroups.Add(index++, list);
            }
            index = 0;
            foreach (var group in template.ConditionGroups)
            {
                var list = new List<bBoosterImplantConditionTemplate>();
                foreach (var condition in group)
                {
                    list.Add(new(BoosterImplantConditionDataBlock.GetBlock(condition)));
                }
                ConditionGroups.Add(index++, list);
            }
        }

        public uint BoosterImplantID { get; set; }
        public int TemplateIndex { get; set; }
        public BoosterImplantCategory ImplantCategory { get; set; }
        public Dictionary<int, List<bBoosterImplantEffectTemplate>> EffectGroups { get; set; } = new();
        public Dictionary<int, List<bBoosterImplantConditionTemplate>> ConditionGroups { get; set; } = new();
    }

    public class bBoosterImplantEffectTemplate
    {
        public bBoosterImplantEffectTemplate(BoosterImplantEffectTemplate effectTemplate)
        {
            ID = effectTemplate.BoosterImplantEffect;
            Effect = BoosterImplantEffectDataBlock.GetBlock(ID).Effect;
            MaxValue = effectTemplate.EffectMaxValue;
            MinValue = effectTemplate.EffectMinValue;
        }

        public uint ID { get; set; }
        public AgentModifier Effect { get; set; }
        public float MaxValue { get; set; }
        public float MinValue { get; set; }
    }

    public class bBoosterImplantConditionTemplate
    {
        public bBoosterImplantConditionTemplate(BoosterImplantConditionDataBlock block)
        {
            ID = block.persistentID;
            Condition = block.Condition;
        }

        public uint ID { get; set; }
        public BoosterCondition Condition { get; set; }
    }

    public static void LoadTemplateData()
    {
        OldBoosterImplantTemplateDataBlocks.Clear();
        OldBoosterImplantTemplateDataBlocks.AddRange(JsonConvert.DeserializeObject<List<BoosterImplantTemplateDataBlock>>(R5BoosterTemplatesJson, new JsonConverter[]
        {
            new LocalizedTextJsonConverter(),
            new ListOfTConverter<uint>(),
            new ListOfTConverter<BoosterImplantEffectInstance>(),
            new ListOfListOfTConverter<BoosterImplantEffectInstance>()
        }));
        BoosterImplantTemplates.Clear();
        var templates = BoosterImplantTemplateDataBlock.GetAllBlocksForEditor();
        for (int i = 0; i < templates.Count; i++)
        {
            BoosterImplantTemplates.Add(new(templates[i]));
        }
        for (int i = 0; i < OldBoosterImplantTemplateDataBlocks.Count; i++)
        {
            BoosterImplantTemplates.Add(new(OldBoosterImplantTemplateDataBlocks[i]));
        }

        BoosterImplantTemplatesLookup.Value = new()
        {
            { BoosterImplantCategory.Muted, new() },
            { BoosterImplantCategory.Bold, new() },
            { BoosterImplantCategory.Aggressive, new() }
        };
        for (int i = 0; i < BoosterImplantTemplates.Count; i++)
        {
            BoosterImplantTemplatesLookup.Value[BoosterImplantTemplates[i].ImplantCategory].Add(new (BoosterImplantTemplates[i]));
        }
        BoosterImplantTemplatesLookup.Save();
    }

    public static void ApplyPerfectBoosterFromTemplate(BoosterImplant boosterImplant, List<BoosterImplantEffectTemplate> effectGroup, List<uint> conditions)
    {
        List<BoosterImplant.Effect> effects = new();
        foreach (var effect in effectGroup)
        {
            effects.Add(new()
            {
                Id = effect.BoosterImplantEffect,
                Value = effect.EffectMaxValue <= 1 && DisableBoosterNegativeEffects ? 1f : (effect.EffectMaxValue - 1f) * BoosterPositiveEffectMultiplier + 1f
            });
        }
        boosterImplant.Effects = effects.ToArray();
        boosterImplant.Conditions = DisableBoosterConditions ? Array.Empty<uint>() : conditions.ToArray();
    }

    public static bool TryGetBoosterImplantTemplate(BoosterImplant boosterImplant, out BoosterImplantTemplate template, out List<BoosterImplantEffectTemplate> effectGroup, out List<uint> conditionGroup, out int effectGroupIndex, out int conditionGroupIndex)
    {
        template = null;
        effectGroup = new();
        conditionGroup = new();
        effectGroupIndex = -1;
        conditionGroupIndex = -1;
        uint persistenID = boosterImplant.TemplateId;
        var templates = BoosterImplantTemplates.FindAll(p => p.BoosterImplantID == persistenID && p.ImplantCategory == boosterImplant.Category);
        for (int k = 0; k < templates.Count; k++)
        {
            if (templates[k].TemplateDataBlock == null)
            {
                continue;
            }
            var conditionGroups = templates[k].ConditionGroups;
            int conditionCount = boosterImplant.Conditions.Count;
            bool ConditionMatch = conditionCount == 0;
            var conditions = boosterImplant.Conditions;
            if (conditionCount > 0)
            {
                for (int i = 0; i < conditionGroups.Count; i++)
                {
                    if (conditionCount != conditionGroups[i].Count)
                    {
                        continue;
                    }

                    bool flag1 = conditions.All(p => conditionGroups[i].Any(q => q == p));
                    bool flag2 = conditionGroups[i].All(p => conditions.Any(q => q == p));
                    if (flag1 && flag2)
                    {
                        ConditionMatch = true;
                        conditionGroup = conditionGroups[i];
                        conditionGroupIndex = i;
                        break;
                    }
                }
            }
            if (!ConditionMatch)
            {
                continue;
            }
            conditionGroupIndex = 0;

            int effectCount = boosterImplant.Effects.Count;
            bool EffectMatch = false;
            var effectGroups = templates[k].EffectGroups;
            var effects = boosterImplant.Effects.ToList();
            for (int i = 0; i < effectGroups.Count; i++)
            {
                if (effectGroups[i].Count != effectCount)
                {
                    continue;
                }
                for (int j = 0; j < effectGroups[i].Count; j++)
                {
                    bool flag1 = effects.All(p => effectGroups[i].Any(q => q.BoosterImplantEffect == p.Id));
                    bool flag2 = effectGroups[i].All(p => effects.Any(q => q.Id == p.BoosterImplantEffect));
                    if (flag1 && flag2)
                    {
                        EffectMatch = true;
                        effectGroup = effectGroups[i];
                        effectGroupIndex = i;
                        break;
                    }
                }
                if (EffectMatch) break;
            }
            if (EffectMatch) return true;
        }
        return false;
    }

    public static List<BoosterImplantTemplate> BoosterImplantTemplates { get; } = new();
    private const string R5BoosterTemplatesJson = "[{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":8,\"MinValue\":1.1,\"MaxValue\":1.13}],\"RandomEffects\":[[{\"BoosterImplantEffect\":7,\"MinValue\":1.1,\"MaxValue\":1.15},{\"BoosterImplantEffect\":50,\"MinValue\":1.1,\"MaxValue\":1.15}]],\"ImplantCategory\":0,\"MainEffectType\":2,\"name\":\"Muted_HealthSupport_Revive\",\"internalEnabled\":true,\"persistentID\":1},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":12,\"MinValue\":1.15,\"MaxValue\":1.25}],\"RandomEffects\":[],\"ImplantCategory\":0,\"MainEffectType\":2,\"name\":\"Muted_HealthSupport_InfectionRes\",\"internalEnabled\":true,\"persistentID\":22},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":3,\"Conditions\":[],\"RandomConditions\":[5,26],\"Effects\":[{\"BoosterImplantEffect\":6,\"MinValue\":1.15,\"MaxValue\":1.25}],\"RandomEffects\":[],\"ImplantCategory\":0,\"MainEffectType\":2,\"name\":\"Muted_Health_RegenSpeed\",\"internalEnabled\":true,\"persistentID\":18},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[5,29],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":10,\"MinValue\":1.05,\"MaxValue\":1.1},{\"BoosterImplantEffect\":11,\"MinValue\":1.05,\"MaxValue\":1.1}]],\"ImplantCategory\":0,\"MainEffectType\":2,\"name\":\"Muted_Health_Single_Resistance\",\"internalEnabled\":true,\"persistentID\":23},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[1,7],\"Effects\":[{\"BoosterImplantEffect\":10,\"MinValue\":1.05,\"MaxValue\":1.1},{\"BoosterImplantEffect\":11,\"MinValue\":1.05,\"MaxValue\":1.1}],\"RandomEffects\":[],\"ImplantCategory\":0,\"MainEffectType\":2,\"name\":\"Muted_Health_Double_Resistance\",\"internalEnabled\":true,\"persistentID\":24},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[1,29],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":54,\"MinValue\":1.05,\"MaxValue\":1.1}]],\"ImplantCategory\":0,\"MainEffectType\":1,\"name\":\"Muted_MeleeDamage\",\"internalEnabled\":true,\"persistentID\":4},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":7,\"Conditions\":[],\"RandomConditions\":[1,7,5],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":52,\"MinValue\":1.05,\"MaxValue\":1.1},{\"BoosterImplantEffect\":53,\"MinValue\":1.05,\"MaxValue\":1.1}]],\"ImplantCategory\":0,\"MainEffectType\":1,\"name\":\"Muted_WeaponDamage\",\"internalEnabled\":true,\"persistentID\":25},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":27,\"MinValue\":1.15,\"MaxValue\":1.25},{\"BoosterImplantEffect\":28,\"MinValue\":1.1,\"MaxValue\":1.15},{\"BoosterImplantEffect\":29,\"MinValue\":1.05,\"MaxValue\":1.1},{\"BoosterImplantEffect\":31,\"MinValue\":1.1,\"MaxValue\":1.15},{\"BoosterImplantEffect\":32,\"MinValue\":1.2,\"MaxValue\":1.3}]],\"ImplantCategory\":0,\"MainEffectType\":4,\"name\":\"Muted_ToolStrength\",\"internalEnabled\":true,\"persistentID\":7},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":40,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":39,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":33,\"MinValue\":1.05,\"MaxValue\":1.1}]],\"ImplantCategory\":0,\"MainEffectType\":4,\"name\":\"Muted_ToolWeakEffects\",\"internalEnabled\":true,\"persistentID\":20},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":34,\"MinValue\":1.2,\"MaxValue\":1.4},{\"BoosterImplantEffect\":35,\"MinValue\":1.05,\"MaxValue\":1.1}],\"RandomEffects\":[],\"ImplantCategory\":0,\"MainEffectType\":3,\"name\":\"Muted_ProcessingSpeed\",\"internalEnabled\":true,\"persistentID\":10},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[7,27],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":41,\"MinValue\":1.13,\"MaxValue\":1.18}]],\"ImplantCategory\":0,\"MainEffectType\":3,\"name\":\"Muted_BioscanSpeed\",\"internalEnabled\":true,\"persistentID\":21},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":1},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":36,\"MinValue\":1.1,\"MaxValue\":1.2},{\"BoosterImplantEffect\":37,\"MinValue\":1.1,\"MaxValue\":1.2},{\"BoosterImplantEffect\":38,\"MinValue\":1.1,\"MaxValue\":1.2},{\"BoosterImplantEffect\":5,\"MinValue\":1.15,\"MaxValue\":1.25}]],\"ImplantCategory\":0,\"MainEffectType\":0,\"name\":\"Muted_InitialState\",\"internalEnabled\":true,\"persistentID\":13},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":7,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":8,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":50,\"MinValue\":1.2,\"MaxValue\":1.3}],\"RandomEffects\":[],\"ImplantCategory\":1,\"MainEffectType\":2,\"name\":\"Bold_HealthSupport_Revive\",\"internalEnabled\":true,\"persistentID\":26},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":12,\"MinValue\":1.4,\"MaxValue\":1.6}],\"RandomEffects\":[],\"ImplantCategory\":1,\"MainEffectType\":2,\"name\":\"Bold_HealthSupport_InfectionRes\",\"internalEnabled\":true,\"persistentID\":27},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[5,26],\"Effects\":[{\"BoosterImplantEffect\":6,\"MinValue\":1.6,\"MaxValue\":2}],\"RandomEffects\":[],\"ImplantCategory\":1,\"MainEffectType\":2,\"name\":\"Bold_Health_RegenSpeed\",\"internalEnabled\":true,\"persistentID\":28},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[5,29],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":10,\"MinValue\":1.15,\"MaxValue\":1.2},{\"BoosterImplantEffect\":11,\"MinValue\":1.15,\"MaxValue\":1.2}]],\"ImplantCategory\":1,\"MainEffectType\":2,\"name\":\"Bold_Health_Single_Resistance\",\"internalEnabled\":true,\"persistentID\":29},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[1,7],\"Effects\":[{\"BoosterImplantEffect\":10,\"MinValue\":1.15,\"MaxValue\":1.2},{\"BoosterImplantEffect\":11,\"MinValue\":1.15,\"MaxValue\":1.2}],\"RandomEffects\":[],\"ImplantCategory\":1,\"MainEffectType\":2,\"name\":\"Bold_Health_Double_Resistance\",\"internalEnabled\":true,\"persistentID\":30},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[1,29],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":54,\"MinValue\":1.2,\"MaxValue\":1.3}]],\"ImplantCategory\":1,\"MainEffectType\":1,\"name\":\"Bold_MeleeDamage\",\"internalEnabled\":true,\"persistentID\":31},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":7,\"Conditions\":[],\"RandomConditions\":[1,7,5],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":52,\"MinValue\":1.15,\"MaxValue\":1.2},{\"BoosterImplantEffect\":53,\"MinValue\":1.15,\"MaxValue\":1.2}]],\"ImplantCategory\":1,\"MainEffectType\":1,\"name\":\"Bold_WeaponDamage\",\"internalEnabled\":true,\"persistentID\":32},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":5,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":27,\"MinValue\":1.4,\"MaxValue\":1.6},{\"BoosterImplantEffect\":28,\"MinValue\":1.2,\"MaxValue\":1.25},{\"BoosterImplantEffect\":29,\"MinValue\":1.15,\"MaxValue\":1.2},{\"BoosterImplantEffect\":31,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":32,\"MinValue\":1.45,\"MaxValue\":1.55}],[{\"BoosterImplantEffect\":39,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":33,\"MinValue\":1.12,\"MaxValue\":1.18},{\"BoosterImplantEffect\":40,\"MinValue\":1.3,\"MaxValue\":1.4}]],\"ImplantCategory\":1,\"MainEffectType\":4,\"name\":\"Bold_ToolStrength\",\"internalEnabled\":true,\"persistentID\":33},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":34,\"MinValue\":1.6,\"MaxValue\":1.8},{\"BoosterImplantEffect\":35,\"MinValue\":1.12,\"MaxValue\":1.18}],\"RandomEffects\":[[{\"BoosterImplantEffect\":49,\"MinValue\":0.91,\"MaxValue\":0.95}]],\"ImplantCategory\":1,\"MainEffectType\":3,\"name\":\"Bold_ProcessingSpeed\",\"internalEnabled\":true,\"persistentID\":35},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[7,27],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":41,\"MinValue\":1.13,\"MaxValue\":1.18}]],\"ImplantCategory\":1,\"MainEffectType\":3,\"name\":\"Bold_BioscanSpeed\",\"internalEnabled\":true,\"persistentID\":36},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":1,\"y\":2},\"DropWeight\":7,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":36,\"MinValue\":1.25,\"MaxValue\":1.35},{\"BoosterImplantEffect\":37,\"MinValue\":1.25,\"MaxValue\":1.35},{\"BoosterImplantEffect\":38,\"MinValue\":1.25,\"MaxValue\":1.35},{\"BoosterImplantEffect\":5,\"MinValue\":1.4,\"MaxValue\":1.6}],[{\"BoosterImplantEffect\":6,\"MinValue\":0.8,\"MaxValue\":0.87},{\"BoosterImplantEffect\":34,\"MinValue\":0.71,\"MaxValue\":0.83}]],\"ImplantCategory\":1,\"MainEffectType\":0,\"name\":\"Bold_InitialState\",\"internalEnabled\":true,\"persistentID\":37},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":7,\"MinValue\":1.4,\"MaxValue\":1.5},{\"BoosterImplantEffect\":8,\"MinValue\":1.8,\"MaxValue\":2},{\"BoosterImplantEffect\":50,\"MinValue\":1.4,\"MaxValue\":1.5}],\"RandomEffects\":[[{\"BoosterImplantEffect\":54,\"MinValue\":0.91,\"MaxValue\":0.95},{\"BoosterImplantEffect\":12,\"MinValue\":0.8,\"MaxValue\":0.87},{\"BoosterImplantEffect\":11,\"MinValue\":0.8,\"MaxValue\":0.87}]],\"ImplantCategory\":2,\"MainEffectType\":2,\"name\":\"Aggressive_HealthSupport_Revive\",\"internalEnabled\":true,\"persistentID\":38},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[{\"BoosterImplantEffect\":12,\"MinValue\":1.8,\"MaxValue\":2}],\"RandomEffects\":[[{\"BoosterImplantEffect\":54,\"MinValue\":0.91,\"MaxValue\":0.95}]],\"ImplantCategory\":2,\"MainEffectType\":2,\"name\":\"Aggressive_HealthSupport_InfectionRes\",\"internalEnabled\":true,\"persistentID\":39},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[5,26],\"Effects\":[{\"BoosterImplantEffect\":6,\"MinValue\":2.2,\"MaxValue\":2.5}],\"RandomEffects\":[],\"ImplantCategory\":2,\"MainEffectType\":2,\"name\":\"Aggressive_Health_RegenSpeed\",\"internalEnabled\":true,\"persistentID\":40},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[5,29],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":10,\"MinValue\":1.3,\"MaxValue\":1.4},{\"BoosterImplantEffect\":11,\"MinValue\":1.3,\"MaxValue\":1.4}]],\"ImplantCategory\":2,\"MainEffectType\":2,\"name\":\"Aggressive_Health_Single_Resistance\",\"internalEnabled\":true,\"persistentID\":41},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":4,\"Conditions\":[],\"RandomConditions\":[1,7],\"Effects\":[{\"BoosterImplantEffect\":10,\"MinValue\":1.3,\"MaxValue\":1.4},{\"BoosterImplantEffect\":11,\"MinValue\":1.3,\"MaxValue\":1.4}],\"RandomEffects\":[],\"ImplantCategory\":2,\"MainEffectType\":2,\"name\":\"Aggressive_Health_Double_Resistance\",\"internalEnabled\":true,\"persistentID\":42},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":1,\"Conditions\":[],\"RandomConditions\":[1,29],\"Effects\":[{\"BoosterImplantEffect\":49,\"MinValue\":1.5,\"MaxValue\":1.6}],\"RandomEffects\":[[{\"BoosterImplantEffect\":11,\"MinValue\":0.83,\"MaxValue\":0.9}]],\"ImplantCategory\":2,\"MainEffectType\":1,\"name\":\"Aggressive_MeleeDamage\",\"internalEnabled\":true,\"persistentID\":43},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":7,\"Conditions\":[],\"RandomConditions\":[1,7,5],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":52,\"MinValue\":1.25,\"MaxValue\":1.3},{\"BoosterImplantEffect\":53,\"MinValue\":1.25,\"MaxValue\":1.3}]],\"ImplantCategory\":2,\"MainEffectType\":1,\"name\":\"Aggressive_WeaponDamage\",\"internalEnabled\":true,\"persistentID\":44},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":6,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":27,\"MinValue\":1.8,\"MaxValue\":2},{\"BoosterImplantEffect\":28,\"MinValue\":1.3,\"MaxValue\":1.4},{\"BoosterImplantEffect\":29,\"MinValue\":1.25,\"MaxValue\":1.3},{\"BoosterImplantEffect\":31,\"MinValue\":1.35,\"MaxValue\":1.4},{\"BoosterImplantEffect\":32,\"MinValue\":1.7,\"MaxValue\":2}],[{\"BoosterImplantEffect\":40,\"MinValue\":1.35,\"MaxValue\":1.4},{\"BoosterImplantEffect\":39,\"MinValue\":1.35,\"MaxValue\":1.4},{\"BoosterImplantEffect\":33,\"MinValue\":1.2,\"MaxValue\":1.3}],[{\"BoosterImplantEffect\":49,\"MinValue\":0.77,\"MaxValue\":0.83},{\"BoosterImplantEffect\":11,\"MinValue\":0.83,\"MaxValue\":0.87},{\"BoosterImplantEffect\":12,\"MinValue\":0.62,\"MaxValue\":0.71}]],\"ImplantCategory\":2,\"MainEffectType\":4,\"name\":\"Aggressive_ToolStrength\",\"internalEnabled\":true,\"persistentID\":45},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":6,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":34,\"MinValue\":2,\"MaxValue\":2.2},{\"BoosterImplantEffect\":35,\"MinValue\":1.2,\"MaxValue\":1.3}],[{\"BoosterImplantEffect\":28,\"MinValue\":1.3,\"MaxValue\":1.4},{\"BoosterImplantEffect\":33,\"MinValue\":1.2,\"MaxValue\":1.3},{\"BoosterImplantEffect\":50,\"MinValue\":1.4,\"MaxValue\":1.5}],[{\"BoosterImplantEffect\":49,\"MinValue\":0.77,\"MaxValue\":0.83},{\"BoosterImplantEffect\":11,\"MinValue\":0.83,\"MaxValue\":0.87}]],\"ImplantCategory\":2,\"MainEffectType\":3,\"name\":\"Aggressive_ProcessingSpeed\",\"internalEnabled\":true,\"persistentID\":47},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":2,\"Conditions\":[],\"RandomConditions\":[7,27],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":41,\"MinValue\":1.2,\"MaxValue\":1.25}]],\"ImplantCategory\":2,\"MainEffectType\":3,\"name\":\"Aggressive_BioscanSpeed\",\"internalEnabled\":true,\"persistentID\":48},{\"Deprecated\":false,\"PublicName\":0,\"Description\":\"\",\"DurationRange\":{\"x\":2,\"y\":3},\"DropWeight\":7,\"Conditions\":[],\"RandomConditions\":[],\"Effects\":[],\"RandomEffects\":[[{\"BoosterImplantEffect\":36,\"MinValue\":1.4,\"MaxValue\":1.53},{\"BoosterImplantEffect\":37,\"MinValue\":1.4,\"MaxValue\":1.53},{\"BoosterImplantEffect\":38,\"MinValue\":1.4,\"MaxValue\":1.53},{\"BoosterImplantEffect\":5,\"MinValue\":1.8,\"MaxValue\":2}],[{\"BoosterImplantEffect\":11,\"MinValue\":0.83,\"MaxValue\":0.87},{\"BoosterImplantEffect\":12,\"MinValue\":0.62,\"MaxValue\":0.71},{\"BoosterImplantEffect\":34,\"MinValue\":0.56,\"MaxValue\":0.62}]],\"ImplantCategory\":2,\"MainEffectType\":0,\"name\":\"Aggressive_InitialState\",\"internalEnabled\":true,\"persistentID\":49}]";
    private static List<BoosterImplantTemplateDataBlock> OldBoosterImplantTemplateDataBlocks = new();

    public class LocalizedTextJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Empty);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return BoosterImplantTemplateDataBlock.UNKNOWN_BLOCK.PublicName;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LocalizedText);
        }
    }

    public class ListOfTConverter<T> : JsonConverter<Il2CppSystem.Collections.Generic.List<T>>
    {
        public override Il2CppSystem.Collections.Generic.List<T> ReadJson(JsonReader reader, Type objectType, Il2CppSystem.Collections.Generic.List<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Array)
            {
                var list = token.ToObject<List<T>>(serializer);
                return list.ToIL2CPPListIfNecessary();
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, Il2CppSystem.Collections.Generic.List<T> value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);
            token.WriteTo(writer);
        }
    }

    public class ListOfListOfTConverter<T> : JsonConverter<Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<T>>>
    {
        public override Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<T>> ReadJson(JsonReader reader, Type objectType, Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<T>> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var list = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<T>>();

            if (token.Type == JTokenType.Array)
            {
                foreach (var innerArrayToken in token.Children())
                {
                    if (innerArrayToken.Type == JTokenType.Array)
                    {
                        var innerList = new Il2CppSystem.Collections.Generic.List<T>();
                        foreach (var itemToken in innerArrayToken.Children())
                        {
                            var item = itemToken.ToObject<T>(serializer);
                            innerList.Add(item);
                        }
                        list.Add(innerList);
                    }
                }
            }

            return list;
        }

        public override void WriteJson(JsonWriter writer, Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<T>> value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);
            token.WriteTo(writer);
        }
    }

    public class BoosterImplantEffectTemplate
    {
        public BoosterImplantEffectTemplate(BoosterImplantEffectInstance effect)
        {
            EffectMaxValue = effect.MaxValue;
            EffectMinValue = effect.MinValue;
            BoosterImplantEffect = effect.BoosterImplantEffect;
        }

        public uint BoosterImplantEffect { get; set; }
        public float EffectMaxValue { get; set; }
        public float EffectMinValue { get; set; }
    }

    public class BoosterImplantTemplate
    {
        public BoosterImplantTemplate(BoosterImplantTemplateDataBlock block)
        {
            TemplateDataBlock = block;

            BoosterImplantID = block.persistentID;
            ImplantCategory = block.ImplantCategory;

            for (int i = 0; i < block.Effects.Count; i++)
            {
                Effects.Add(new(block.Effects[i]));
            }
            for (int i = 0; i < block.RandomEffects.Count; i++)
            {
                List<BoosterImplantEffectTemplate> randomEffects = new();
                for (int j = 0; j < block.RandomEffects[i].Count; j++)
                {
                    randomEffects.Add(new(block.RandomEffects[i][j]));
                }
                RandomEffects.Add(randomEffects);
            }

            Conditions.AddRange(block.Conditions.ToArray());
            RandomConditions.AddRange(block.RandomConditions.ToArray());

            EffectGroups = GenerateEffectGroups();
            ConditionGroups = GenerateConditionGroups();
        }

        private List<List<BoosterImplantEffectTemplate>> GenerateEffectGroups()
        {
            List<List<BoosterImplantEffectTemplate>> effectGroups = new();

            List<List<BoosterImplantEffectTemplate>> combinations = GetNElementCombinations(RandomEffects);
            for (int i = 0; i < combinations.Count; i++)
            {
                List<BoosterImplantEffectTemplate> effectGroup = Effects.ToList();
                effectGroup.AddRange(combinations[i]);
                effectGroups.Add(effectGroup);
            }
            return effectGroups;
        }

        private List<List<uint>> GenerateConditionGroups()
        {
            List<List<uint>> conditions = new() { Conditions, RandomConditions };
            List<List<uint>> combinations = GetNElementCombinations(conditions);
            return combinations;
        }

        private static List<List<T>> GetNElementCombinations<T>(List<List<T>> lists)
        {
            List<List<T>> combinations = new List<List<T>>();

            GetNElementCombinationsHelper(lists, new List<T>(), 0, combinations);

            return combinations;
        }

        private static void GetNElementCombinationsHelper<T>(List<List<T>> lists, List<T> currentCombination, int currentIndex, List<List<T>> combinations)
        {
            if (currentIndex == lists.Count)
            {
                combinations.Add(new List<T>(currentCombination));
                return;
            }

            List<T> currentList = lists[currentIndex];

            if (currentList.Count == 0)
            {
                GetNElementCombinationsHelper(lists, currentCombination, currentIndex + 1, combinations);
                return;
            }

            foreach (T item in currentList)
            {
                currentCombination.Add(item);
                GetNElementCombinationsHelper(lists, currentCombination, currentIndex + 1, combinations);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }

        public uint BoosterImplantID { get; set; }
        public BoosterImplantCategory ImplantCategory { get; set; }
        [JsonIgnore]
        public List<BoosterImplantEffectTemplate> Effects { get; set; } = new();
        [JsonIgnore]
        public List<List<BoosterImplantEffectTemplate>> RandomEffects { get; set; } = new();
        [JsonIgnore]
        public List<uint> Conditions { get; set; } = new();
        [JsonIgnore]
        public List<uint> RandomConditions { get; set; } = new();

        [JsonIgnore]
        public BoosterImplantTemplateDataBlock TemplateDataBlock { get; private set; } = null;

        public List<List<BoosterImplantEffectTemplate>> EffectGroups { get; set; } = new();
        public List<List<uint>> ConditionGroups { get; set; } = new();
    }
}
