using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

namespace Vbm.Valheim.ItemConfigurator
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.bepinex.plugins.atosarrows", "0.6.0")]
    public class Main : BaseUnityPlugin
    {
        public const string Version = "0.0.1";
        public const string Name = "VBM Valheim Item Configurator";
        public const string Guid = "vbm.valheim.itemconfigurator";
        public const string Namespace = "Vbm.Valheim.ItemConfigurator";
        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Guid);
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    public class Weapon
    {
        public string name { get; set; }
        public int skillType { get; set; }
        public string craftingStation { get; set; }
        public int minStationLevel { get; set; }
        public int maxQuality { get; set; }
        public int amount { get; set; }
        public string repairStation { get; set; }
        public List<string> reqs { get; set; }
        public List<string> upgradeReqs { get; set; }
        public List<string> durabilityEffects { get; set; }
        public List<string> holdEffects { get; set; }
        public List<string> elementalEffects { get; set; }
        public List<string> physicalEffects { get; set; }
        public List<string> defenseEffects { get; set; }
        public List<string> upgradesPerLevel { get; set; }
        public string ammoType { get; set; }
    }

    public class Patch
    {
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public class PatchObjectDBAwake
        {
            [UsedImplicitly]
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyAfter("com.bepinex.plugins.jotunnlib", "com.bepinex.plugins.atosarrows")]
            public static void Postfix()
            {
                ZLog.Log($"{Main.Namespace}.{MethodBase.GetCurrentMethod().DeclaringType?.Name}.{MethodBase.GetCurrentMethod().Name}");
                FixRecipes();
            }
        }

        private static void FixRecipes()
        {
            ZLog.Log("Updating Recipes");

            // This is normal checking if ObjectDB is ready. 
            if (!(ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null))
            {
                ZLog.Log("ObjectDBReady not ready - skipping");
                return;
            }

            // com.bepinex.plugins.atosarrows waits to load until the Active Scene is 'main'. Most other item mods load on Awake and CopyOtherDB
            if (SceneManager.GetActiveScene().name != "main")
            {
                ZLog.Log("Not at 'main' Scene - skipping");
                return;
            }

            // We need to know the ItemId of the item we want to change. For the Crossbow the ItemId is 'XBow'.
            // This foreach should return any Recipe that results in creating a XBow. This should solve an 
            // edge case where an item has more then one Recipe to craft it.
            foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == "XBow"))
            {
                Weapon weapon = JsonConvert.DeserializeObject<Weapon>(File.ReadAllText(@"test.json"));
                ZLog.Log($"{weapon.name}");
                ZLog.Log($"{weapon.maxQuality}");
                weapon.upgradeReqs.ForEach(i => ZLog.Log($"{0}\n"));
                instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.maxQuality; // Sets the max level an item can be upgraded to.
                ZLog.Log($"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_maxQuality to {instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality}");

                //ObjectDB.instance.m_items
                //instanceMRecipe.m_item.m_itemData.m_shared.m_durabilityPerLevel = weapon.durabilityEffects

                foreach (Piece.Requirement requirement in instanceMRecipe.m_resources)
                {
                    switch (requirement.m_resItem.name)
                    {
                        case "Crystal":
                            requirement.m_amountPerLevel = 4;
                            break;

                        case "BlackMetal":
                            requirement.m_amountPerLevel = 30;
                            break;

                        case "FineWood":
                            requirement.m_amountPerLevel = 4;
                            break;

                        case "LinenThread":
                            requirement.m_amountPerLevel = 4;
                            break;
                    }
                }
            }
        }
    }
}
