using System;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.IO;
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
        public List<string> sharedStats { get; set; }
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
            BindingFlags bindingFlags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Static;

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

            Weapon weapon = fastJSON.JSON.ToObject<Weapon>(File.ReadAllText($"{BepInEx.Paths.PluginPath}/valheim-dynamic-items/test.json"));
            foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == weapon.name))
            {
                ZLog.Log($"{weapon.name}");
                ZLog.Log($"{weapon.maxQuality}");
                weapon.upgradeReqs.ForEach(i => ZLog.Log($"{i}{Environment.NewLine}"));
                instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.maxQuality; // Sets the max level an item can be upgraded to.
                ZLog.Log($"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_maxQuality to {instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality}");


                foreach (string updatePiece in weapon.upgradeReqs)
                {
                    string[] parsedUpddatePiece = updatePiece.Split(':');
                    foreach (Piece.Requirement requirement in instanceMRecipe.m_resources)
                    {
                        ZLog.Log($"{requirement.m_resItem.name}{Environment.NewLine}");
                        if (parsedUpddatePiece.GetValue(0).Equals(requirement.m_resItem.name))
                        {
                            requirement.m_amountPerLevel = Convert.ToInt32(parsedUpddatePiece.GetValue(1));
                            ZLog.Log($"The item we want to modify: {parsedUpddatePiece.GetValue(0)}{Environment.NewLine}");
                            ZLog.Log($"Material modifed: {requirement.m_resItem.name}{Environment.NewLine}");
                            ZLog.Log($"New material requiredment: {requirement.m_amountPerLevel}{Environment.NewLine}");
                        }
                    }
                }

                foreach (string effects in weapon.sharedStats)
                {
                    foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields(bindingFlags))
                    {

                        
                        string[] parsedEffects = effects.Split(':');
                        //ZLog.Log($"{field.Name.ToString()}");
                        string affixedName = parsedEffects.GetValue(0).ToString();
                        if(parsedEffects.GetValue(0).ToString().StartsWith("m_") == false)
                        {
                            affixedName = "m_" + affixedName;
                        }
                        if (affixedName.ToString().Equals(field.Name.ToString()))
                        {
                            field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, parsedEffects.GetValue(1));
                            if(affixedName == "m_attackForce")
                            {
                                ZLog.Log($"{instanceMRecipe.m_item.m_itemData.m_shared.m_attackForce}{Environment.NewLine}");
                            }
                        }
                    }
                }
            }
        }
    }
}
