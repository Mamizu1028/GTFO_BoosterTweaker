using GameData;
using TheArchive.Core.ModulesAPI;
using static Hikaria.BoosterTweaker.Managers.BoosterImplantTemplateManager;

namespace Hikaria.BoosterTweaker.Managers;

public static class CustomPerfectBoosterImplantManager
{
    public static CustomSetting<Dictionary<BoosterImplantCategory, List<CustomPerfectBoosterImplant>>> CustomPerfectBoosterImplants { get; set; } = new("CustomPerfectBoosterImplants", new()
    {
        { BoosterImplantCategory.Muted, new() },
        { BoosterImplantCategory.Bold, new() },
        { BoosterImplantCategory.Aggressive, new() }
    }, null, LoadingTime.AfterGameDataInited);

    public static void CreateCustomPerfectBoosterImplantsFromInventory()
    {
        CustomPerfectBoosterImplants.Value = new()
        {
            { BoosterImplantCategory.Muted, new() },
            { BoosterImplantCategory.Bold, new() },
            { BoosterImplantCategory.Aggressive, new() }
        };
        foreach (var category in PersistentInventoryManager.Current.m_boosterImplantInventory.Categories)
        {
            foreach (var item in category.Inventory)
            {
                CustomPerfectBoosterImplants.Value[item.Implant.Category].Add(new(item.Implant));
            }
        }
    }

    public static void ApplyCustomPerfectBoosterImplants()
    {
        uint Id = 7225460U;
        for (int i = 0; i < 3; i++)
        {
            var inventory = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i].Inventory;
            inventory.Clear();
            var category = (BoosterImplantCategory)i;
            for (int j = 0; j < CustomPerfectBoosterImplants.Value[category].Count; j++)
            {
                var CustomPerfectBoosterImplant = CustomPerfectBoosterImplants.Value[category][j];
                if (!CustomPerfectBoosterImplant.Enabled)
                {
                    continue;
                }
                if (BoosterImplantTemplateDataBlock.GetBlock(CustomPerfectBoosterImplant.TemplateId) == null)
                {
                    continue;
                }
                var templates = BoosterImplantTemplates.FindAll(p => p.BoosterImplantID == CustomPerfectBoosterImplant.TemplateId);
                if (!templates.Any() || CustomPerfectBoosterImplant.TemplateIndex <= -1 || CustomPerfectBoosterImplant.TemplateIndex >= templates.Count)
                {
                    continue;
                }
                var template = templates[CustomPerfectBoosterImplant.TemplateIndex];
                if (template == null || template.BoosterImplantID == 0
                    || CustomPerfectBoosterImplant.ConditionGroupIndex >= template.ConditionGroups.Count
                    || CustomPerfectBoosterImplant.EffectGroupIndex >= template.EffectGroups.Count
                    || (CustomPerfectBoosterImplant.ConditionGroupIndex <= -1)
                    || CustomPerfectBoosterImplant.EffectGroupIndex <= -1)
                {
                    continue;
                }
                List<DropServer.BoosterImplants.BoosterImplantEffect> effects = new();
                foreach (var effect in template.EffectGroups[CustomPerfectBoosterImplant.EffectGroupIndex])
                {
                    effects.Add(new() { Id = effect.BoosterImplantEffect, Param = effect.EffectMaxValue });
                }
                var item = new BoosterImplantInventoryItem(new DropServer.BoosterImplants.BoosterImplantInventoryItem()
                {
                    Conditions = template.ConditionGroups[CustomPerfectBoosterImplant.ConditionGroupIndex].ToArray(),
                    Effects = effects.ToArray(),
                    Id = Id,
                    TemplateId = CustomPerfectBoosterImplant.TemplateId,
                    Flags = 1U,
                });
                inventory.Add(item);
                item.Implant.InstanceId = Id;
                item.Implant.Uses = (int)item.Implant.Template.DurationRange.y;
                CustomPerfectBoosterImplant.Name = item.Implant.GetCompositPublicName(true);
                Id++;
            }
        }
    }

    public class CustomPerfectBoosterImplant
    {
        public CustomPerfectBoosterImplant(BoosterImplant implant)
        {
            Name = implant.GetCompositPublicName(true);
            TemplateId = implant.TemplateId;
            Category = implant.Category;
            if (TryGetBoosterImplantTemplate(implant, out _, out _, out _, out var effectGroupIndex, out var conditionGroupIndex))
            {
                ConditionGroupIndex = conditionGroupIndex;
                EffectGroupIndex = effectGroupIndex;
            }
            Enabled = true;
        }
        public CustomPerfectBoosterImplant()
        {
        }

        public string Name { get; set; } = string.Empty;
        public BoosterImplantCategory Category { get; set; } = BoosterImplantCategory._COUNT;
        public uint TemplateId { get; set; } = 0;
        public int TemplateIndex { get; set; } = 0;
        public int EffectGroupIndex { get; set; } = 0;
        public int ConditionGroupIndex { get; set; } = 0;
        public bool Enabled { get; set; }
    }
}