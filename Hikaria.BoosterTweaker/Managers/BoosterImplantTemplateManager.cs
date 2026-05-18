using BoosterImplants;
using Clonesoft.Json;
using Clonesoft.Json.Linq;
using GameData;
using Localization;
using System.Reflection;
using TheArchive.Core.ModulesAPI;
using TheArchive.Utilities;

namespace Hikaria.BoosterTweaker.Managers;

public static class BoosterImplantTemplateManager
{
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
        var asm = Assembly.GetExecutingAssembly();
        using Stream stream = asm.GetManifestResourceStream("Hikaria.BoosterTweaker.Resources.OldBoosterTemplates.json");
        using var reader = new StreamReader(stream);
        string OldBoosterTemplatesJson = reader.ReadToEnd();
        OldBoosterImplantTemplateDataBlocks.Clear();
        OldBoosterImplantTemplateDataBlocks.AddRange(JsonConvert.DeserializeObject<List<BoosterImplantTemplateDataBlock>>(OldBoosterTemplatesJson, new JsonConverter[]
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
                Value = effect.EffectMaxValue <= 1 && DisableBoosterNegativeEffects ? 1f : effect.EffectMaxValue
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
