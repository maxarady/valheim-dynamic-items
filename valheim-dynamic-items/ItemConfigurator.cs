using System;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

namespace Vbm.Valheim.ItemConfigurator
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency("com.bepinex.plugins.atosarrows", "0.6.0")]
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
            ItemManager.OnItemsRegistered += Fix;
            ItemManager.OnItemsRegisteredFejd += Fix;
        }

        private void Fix()
        {
            Patch.PatchObjectDBAwake.Postfix();
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
            [HarmonyAfter("com.bepinex.plugins.jotunnlib")]
            public static void Postfix()
            {
                ZLog.Log($"{Main.Namespace}.{MethodBase.GetCurrentMethod().DeclaringType?.Name}.{MethodBase.GetCurrentMethod().Name}");
                FixRecipes();
            }
        }

        private static void FixRecipes()
        {
            int debug = 0;

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
                    ZLog.Log("This concludes debug block 1.");
                }

                foreach (ItemDrop.ItemData.SharedData list in (
                    from i in ObjectDB.instance.m_items
                    select i.GetComponent<ItemDrop>().m_itemData.m_shared).ToList<ItemDrop.ItemData.SharedData>())
                {
                    //File.WriteAllText($"{list.m_name}.json" ,fastJSON.JSON.ToNiceJSON(list));
                    //ZLog.Log($"{list.m_name}");
                    if (list.m_name.Equals(weapon.name) && ((int)list.m_itemType == 2))
                    {
                        ZLog.Log("Yay!");
                        foreach (string effects in weapon.sharedStats)
                        {
                            ZLog.Log("Fucking nailing it.");
                            foreach (FieldInfo field in list.GetType().GetFields(bindingFlags))
                            {
                                string[] parsedEffects = effects.Split(':');
                                string affixedName = parsedEffects.GetValue(0).ToString();
                                if (parsedEffects.GetValue(0).ToString().StartsWith("m_") == false)
                                {
                                    affixedName = "m_" + affixedName;
                                }
                                if (affixedName.ToString().Equals(field.Name.ToString()))
                                {
                                    //ZLog.Log(affixedName);
                                    //ZLog.Log("Fucking nailed it.");
                                    string sysField = field.FieldType.ToString();
                                    string[] parsedSysField = sysField.Split('.');
                                    if (parsedSysField.GetValue(1).Equals("Boolean"))
                                    {
                                        //ZLog.Log($"This is bolean: {field.Name}");
                                        var isBool = bool.TryParse(parsedEffects.GetValue(1).ToString(), out bool p);
                                        field.SetValue(list, p);
                                        //ZLog.Log(field.Name);
                                    }
                                    else if (parsedSysField.GetValue(1).Equals("Single"))
                                    {
                                        //ZLog.Log($"This is Single: {field.Name}");
                                        var isSingle = Single.TryParse(parsedEffects.GetValue(1).ToString(), out Single q);
                                        field.SetValue(list, q);
                                        //ZLog.Log(field.Name);
                                    }
                                    else if (parsedSysField.GetValue(1).Equals("Double"))
                                    {
                                        //ZLog.Log($"This is Double: {field.Name}");
                                        var isDouble = double.TryParse(parsedEffects.GetValue(1).ToString(), out double d);
                                        field.SetValue(list, d);
                                        //ZLog.Log(field.Name);
                                    }
                                    else if (parsedSysField.GetValue(1).Equals("String"))
                                    {
                                        //ZLog.Log($"This is String: {field.Name}");
                                        field.SetValue(list, parsedEffects.GetValue(1));
                                        //ZLog.Log(field.Name);
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
                /*ZLog.Log($"=============================================================================================");
                foreach (var list in (
                    from i in ObjectDB.instance.m_recipes
                    select i.m_item))
                {
                    ZLog.Log($"{list.name}");
                }*/

                foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name != "null"))
                {
                    ZLog.Log($"Loops: {instanceMRecipe.m_item.name}");
                }

                foreach (Recipe instanceMRecipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == weapon.name))
                {
                    //ZLog.Log($"{weapon.name}");
                    ZLog.Log($"{instanceMRecipe.name}");

                    instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.maxQuality;
                    instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.minStationLevel;
                    foreach (string requiredPiece in weapon.reqs)
                    {
                        string[] parsedRequiredPiece = requiredPiece.Split(':');
                        foreach (Piece.Requirement requirement in instanceMRecipe.m_resources)
                        {
                            ZLog.Log($"{requirement.m_resItem.name}{Environment.NewLine}");
                            if (parsedRequiredPiece.GetValue(0).Equals(requirement.m_resItem.name))
                            {
                                requirement.m_amount = Convert.ToInt32(parsedRequiredPiece.GetValue(1));
                            }
                        }
                    }

                    foreach (string updatePiece in weapon.upgradeReqs)
                    {
                        string[] parsedUpddatePiece = updatePiece.Split(':');
                        foreach (Piece.Requirement requirement in instanceMRecipe.m_resources)
                        {
                            ZLog.Log($"{requirement.m_resItem.name}{Environment.NewLine}");
                            if (parsedUpddatePiece.GetValue(0).Equals(requirement.m_resItem.name))
                            {
                                ZLog.Log("In the resItem loop");
                                requirement.m_amountPerLevel = Convert.ToInt32(parsedUpddatePiece.GetValue(1));
                                ZLog.Log($"In the resItem loop value is ${requirement.m_amountPerLevel}");
                            }
                        }
                    }

                    foreach (string effects in weapon.sharedStats)
                    {
                        foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.GetType().GetFields(bindingFlags))
                        {
                            string[] parsedEffects = effects.Split(':');
                            string affixedName = parsedEffects.GetValue(0).ToString();
                            if (parsedEffects.GetValue(0).ToString().StartsWith("m_") == false)
                            {
                                affixedName = "m_" + affixedName;
                            }
                            if (affixedName.ToString().Equals(field.Name.ToString()))
                            {
                                ZLog.Log(affixedName);
                                string sysField = field.FieldType.ToString();
                                string[] parsedSysField = sysField.Split('.');
                                var m_shared = instanceMRecipe.m_item.m_itemData;
                                var affixedNameField = m_shared.GetType().GetField("m_shared");
                                object affixedNameValue = affixedNameField.GetValue(m_shared);
                                var codeField = affixedNameValue.GetType().GetField(affixedName);
                                if (parsedSysField.GetValue(1).Equals("Boolean"))
                                {
                                    var isBool = bool.TryParse(parsedEffects.GetValue(1).ToString(), out bool p);
                                    field.SetValue(m_shared, p);
                                    ZLog.Log(field.Name);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedEffects.GetValue(1).ToString(), out Single q);
                                    field.SetValue(m_shared, q);
                                    ZLog.Log(field.Name);
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedEffects.GetValue(1).ToString(), out double d);
                                    field.SetValue(m_shared, d);
                                    ZLog.Log(field.Name);
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    field.SetValue(m_shared, parsedEffects.GetValue(1));
                                    ZLog.Log(field.Name);
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
                                    ZLog.Log($"DMS: The field was a Boolean, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {p} and this is actual value ");
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedDamages.GetValue(1).ToString(), out Single q);
                                    codeField.SetValue(affixedNameValue, q);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DMS: The field was a Single, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {q}");
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedDamages.GetValue(1).ToString(), out double d);
                                    codeField.SetValue(affixedNameValue, d);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DMS: The field was a Double, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {d}");
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    codeField.SetValue(affixedNameValue, parsedDamages.GetValue(1));
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DMS: The field was a String, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {parsedDamages.GetValue(1)}");
                                }
                                else
                                {
                                    ZLog.Log("No matched types");
                                }
                                if (affixedName == "m_slash") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_slash.ToString()}");
                                if (affixedName == "m_pierce") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_pierce.ToString()}");
                                if (affixedName == "m_blunt") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_blunt.ToString()}");
                                if (affixedName == "m_fire") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_fire.ToString()}");
                                if (affixedName == "m_frost") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_frost.ToString()}");
                                if (affixedName == "m_lightning") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_lightning.ToString()}");
                                if (affixedName == "m_spirit") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_spirit.ToString()}");
                                if (affixedName == "m_pickaxe") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_pickaxe.ToString()}");
                                if (affixedName == "m_chop") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damages.m_chop.ToString()}");
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
                                    ZLog.Log($"DML: The field was a Boolean, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {p}");
                                }
                                else if (parsedSysField.GetValue(1).Equals("Single"))
                                {
                                    var isSingle = Single.TryParse(parsedDamagesPerLevel.GetValue(1).ToString(), out Single q);
                                    codeField.SetValue(affixedNameValue, q);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DML: The field was a single, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {q}");
                                }
                                else if (parsedSysField.GetValue(1).Equals("Double"))
                                {
                                    var isDouble = double.TryParse(parsedDamagesPerLevel.GetValue(1).ToString(), out double d);
                                    codeField.SetValue(affixedNameValue, d);
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DML: The field was a Double, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {d}");
                                }
                                else if (parsedSysField.GetValue(1).Equals("String"))
                                {
                                    codeField.SetValue(affixedNameValue, parsedDamagesPerLevel.GetValue(1));
                                    affixedNameField.SetValue(m_shared, affixedNameValue);
                                    ZLog.Log($"DML: The field was a String, and affix name was {affixedName} while the code field was {codeField.Name.ToString()} and the value was {parsedDamagesPerLevel.GetValue(1)}");
                                }
                                else
                                {
                                    ZLog.Log("No matched types");
                                }
                                if (affixedName == "m_slash") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_slash.ToString()}");
                                if (affixedName == "m_pierce") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_pierce.ToString()}");
                                if (affixedName == "m_blunt") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_blunt.ToString()}");
                                if (affixedName == "m_fire") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_fire.ToString()}");
                                if (affixedName == "m_frost") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_frost.ToString()}");
                                if (affixedName == "m_lightning") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_lightning.ToString()}");
                                if (affixedName == "m_spirit") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_spirit.ToString()}");
                                if (affixedName == "m_pickaxe") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_pickaxe.ToString()}");
                                if (affixedName == "m_chop") ZLog.Log($"This is {affixedName}: {instanceMRecipe.m_item.m_itemData.m_shared.m_damagesPerLevel.m_chop.ToString()}");
                            }
                        }
                    }
                }
            }
        }
    }
}
