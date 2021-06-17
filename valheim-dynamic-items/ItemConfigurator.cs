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
        public List<string> damagesStats { get; set; }
        public List<string> damagesPerLevelStats { get; set; }
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
            int debug = 1;

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

            string[] files = Directory.GetFiles($"{BepInEx.Paths.PluginPath}\\valheim-dynamic-items", "*.json");
            ZLog.Log(files.Length);
            foreach (var file in files)
            {
                ZLog.Log(file);
                Weapon weapon = fastJSON.JSON.ToObject<Weapon>(File.ReadAllText(file));
                if (debug == 1)
                {
                    ZLog.Log(weapon.name);
                    ZLog.Log(weapon.maxQuality);
                    ZLog.Log("This concludes debug block 1.");
                }

                foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == weapon.name))
                {
                    ZLog.Log($"{weapon.name}");
                    weapon.upgradeReqs.ForEach(i => ZLog.Log($"{i}{Environment.NewLine}"));
                    instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.maxQuality; // Sets the max level an item can be upgraded to.

                    foreach (string updatePiece in weapon.upgradeReqs)
                    {
                        string[] parsedUpddatePiece = updatePiece.Split(':');
                        foreach (Piece.Requirement requirement in instanceMRecipe.m_resources)
                        {
                            ZLog.Log($"{requirement.m_resItem.name}{Environment.NewLine}");
                            if (parsedUpddatePiece.GetValue(0).Equals(requirement.m_resItem.name))
                            {
                                requirement.m_amountPerLevel = Convert.ToInt32(parsedUpddatePiece.GetValue(1));
                                /*ZLog.Log($"The item we want to modify: {parsedUpddatePiece.GetValue(0)}{Environment.NewLine}");
                                ZLog.Log($"Material modifed: {requirement.m_resItem.name}{Environment.NewLine}");
                                ZLog.Log($"New material requiredment: {requirement.m_amountPerLevel}{Environment.NewLine}");*/
                            }
                        }
                    }
                    foreach (string effects in weapon.sharedStats)
                    {
                        foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields(bindingFlags))
                        {
                            string[] parsedEffects = effects.Split(':');
                            string affixedName = parsedEffects.GetValue(0).ToString();
                            if (parsedEffects.GetValue(0).ToString().StartsWith("m_") == false)
                            {
                                affixedName = "m_" + affixedName;
                            }
                            if (affixedName.ToString().Equals(field.Name.ToString()))
                            {
                                string sysField = field.FieldType.ToString();
                                string[] parsedSysField = sysField.Split('.');
                                if (parsedSysField.GetValue(1).Equals("Boolean"))
                                {
                                    var isBool = bool.TryParse(parsedEffects.GetValue(1).ToString(), out bool p);
                                    field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, p);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedEffects.GetValue(1).ToString(), out Single q);
                                    field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, q);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedEffects.GetValue(1).ToString(), out double d);
                                    field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, d);
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, parsedEffects.GetValue(1));
                                }
                                else
                                {
                                    ZLog.Log("No matched types");
                                }
                            }
                        }
                    }
                    foreach (string damages in weapon.damagesStats)
                    {
                        foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.m_damages.GetType().GetFields(bindingFlags))
                        {
                            string[] parsedDamages = damages.Split(':');
                            string affixedName = parsedDamages.GetValue(0).ToString();
                            if (parsedDamages.GetValue(0).ToString().StartsWith("m_") == false)
                            {
                                affixedName = "m_" + affixedName;
                            }
                            if (affixedName.ToString().Equals(field.Name.ToString()))
                            {
                                string sysField = field.FieldType.ToString();
                                string[] parsedSysField = sysField.Split('.');
                                var m_shared = instanceMRecipe.m_item.m_itemData.m_shared;
                                var affixedNameField = m_shared.GetType().GetField("m_damages");
                                object affixedNameValue = affixedNameField.GetValue(m_shared);
                                var codeField = affixedNameValue.GetType().GetField(affixedName);
                                if (parsedSysField.GetValue(1).Equals("Boolean"))
                                {
                                    var isBool = bool.TryParse(parsedDamages.GetValue(1).ToString(), out bool p);
                                    codeField.SetValue(affixedNameValue, p);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedDamages.GetValue(1).ToString(), out Single q);
                                    codeField.SetValue(affixedNameValue, q);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedDamages.GetValue(1).ToString(), out double d);
                                    codeField.SetValue(affixedNameValue, d);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    codeField.SetValue(affixedNameValue, parsedDamages.GetValue(1));
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else
                                {
                                    ZLog.Log("No matched types");
                                }
                            }
                        }
                    }
                    foreach (string damagesPerLevel in weapon.damagesPerLevelStats)
                    {
                        foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.GetType().GetFields(bindingFlags))
                        {
                            string[] parsedDamagesPerLevel = damagesPerLevel.Split(':');
                            string affixedName = parsedDamagesPerLevel.GetValue(0).ToString();
                            if (parsedDamagesPerLevel.GetValue(0).ToString().StartsWith("m_") == false)
                            {
                                affixedName = "m_" + affixedName;
                            }
                            var m_shared = instanceMRecipe.m_item.m_itemData.m_shared;
                            var affixedNameField = m_shared.GetType().GetField("m_damagesPerLevel");
                            object affixedNameValue = affixedNameField.GetValue(m_shared);
                            var codeField = affixedNameValue.GetType().GetField(affixedName);
                            if (affixedName.ToString().Equals(field.Name.ToString()))
                            {
                                string sysField = field.FieldType.ToString();
                                string[] parsedSysField = sysField.Split('.');
                                if (parsedSysField.GetValue(1).Equals("Boolean"))
                                {
                                    var isBool = bool.TryParse(parsedDamagesPerLevel.GetValue(1).ToString(), out bool p);
                                    codeField.SetValue(affixedNameValue, p);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedDamagesPerLevel.GetValue(1).ToString(), out Single q);
                                    codeField.SetValue(affixedNameValue, q);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedDamagesPerLevel.GetValue(1).ToString(), out double d);
                                    codeField.SetValue(affixedNameValue, d);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    codeField.SetValue(affixedNameValue, parsedDamagesPerLevel.GetValue(1));
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                }
                                else
                                {
                                    ZLog.Log("No matched types");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
