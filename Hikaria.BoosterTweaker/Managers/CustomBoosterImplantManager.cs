using GameData;
using TheArchive.Core.ModulesAPI;

namespace Hikaria.BoosterTweaker.Managers
{
    public static class CustomBoosterImplantManager
    {
        public static CustomSetting<Dictionary<BoosterImplantCategory, List<CustomBoosterImplant>>> CustomBoosterImplants { get; set; } = new("CustomBoosterImplants",
        new()
        {
            { BoosterImplantCategory.Muted, new() },
            { BoosterImplantCategory.Bold, new() },
            { BoosterImplantCategory.Aggressive, new() }
        });

        public static void CreateCustomBoosterImplantsFromInventory()
        {
            CustomBoosterImplants.Value = new()
            {
                { BoosterImplantCategory.Muted, new() },
                { BoosterImplantCategory.Bold, new() },
                { BoosterImplantCategory.Aggressive, new() }
            };
            foreach (var category in PersistentInventoryManager.Current.m_boosterImplantInventory.Categories)
            {
                foreach (var item in category.Inventory)
                {
                    CustomBoosterImplants.Value[item.Implant.Category].Add(new(item.Implant));
                }
            }
        }

        public static void ApplyCustomBoosterImplants()
        {
            uint Id = 3223718U;
            for (int i = 0; i < 3; i++)
            {
                var inventory = PersistentInventoryManager.Current.m_boosterImplantInventory.Categories[i].Inventory;
                inventory.Clear();
                var category = (BoosterImplantCategory)i;
                for (int j = 0; j < CustomBoosterImplants.Value[category].Count; j++)
                {
                    var CustomBoosterImplant = CustomBoosterImplants.Value[category][j];
                    if (!CustomBoosterImplant.Enabled)
                    {
                        continue;
                    }
                    if (BoosterImplantTemplateDataBlock.GetBlock(CustomBoosterImplant.TemplateId) == null)
                    {
                        continue;
                    }
                    if (CustomBoosterImplant.Effects.Any(p => p.Id == 0) || CustomBoosterImplant.Conditions.Any(p => p == 0))
                    {
                        continue;
                    }
                    List<DropServer.BoosterImplants.BoosterImplantEffect> effects = new();
                    foreach (var effect in CustomBoosterImplant.Effects)
                    {
                        effects.Add(new() { Id = effect.Id, Param = effect.Value });
                    }
                    var item = new BoosterImplantInventoryItem(new DropServer.BoosterImplants.BoosterImplantInventoryItem()
                    {
                        Conditions = CustomBoosterImplant.Conditions.ToArray(),
                        Effects = effects.ToArray(),
                        Id = Id,
                        TemplateId = CustomBoosterImplant.TemplateId,
                        Flags = 1U,
                    });
                    inventory.Add(item);
                    item.Implant.InstanceId = Id;
                    item.Implant.Uses = (int)item.Implant.Template.DurationRange.y;
                    CustomBoosterImplant.Name = item.Implant.GetCompositPublicName(true);
                    Id++;
                }
            }
        }

        public class CustomBoosterImplant
        {
            public CustomBoosterImplant(BoosterImplant implant)
            {
                Name = implant.GetCompositPublicName(true);
                TemplateId = implant.TemplateId;
                Conditions = implant.Conditions.ToList();
                Category = implant.Category;
                Effects.Clear();
                foreach (var effect in implant.Effects)
                {
                    Effects.Add(new(effect));
                }
                Enabled = true;
            }
            public CustomBoosterImplant()
            {
            }

            public string Name { get; set; } = string.Empty;
            public BoosterImplantCategory Category { get; set; } = BoosterImplantCategory._COUNT;
            public uint TemplateId { get; set; } = 0;
            public List<uint> Conditions { get; set; } = new();
            public List<Effect> Effects { get; set; } = new();
            public bool Enabled { get; set; } = false;

            public class Effect
            {
                public Effect(BoosterImplant.Effect effect)
                {
                    Id = effect.Id;
                    Value = effect.Value;
                }
                public Effect()
                {
                }

                public uint Id { get; set; } = 0;
                public float Value { get; set; } = 1f;
            }
        }
    }
}
