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

                //Recipe item = ObjectDB.instance.m_recipes.
                //instanceMRecipe.m_item.m
              
                //Weapon weapon = fastJSON.JSON.ToObject<Weapon>(File.ReadAllText($"{BepInEx.Paths.PluginPath}/valheim-dynamic-items/test.json"));
                ZLog.Log($"{weapon.name}");
                ZLog.Log($"{weapon.maxQuality}");
                weapon.upgradeReqs.ForEach(i => ZLog.Log($"{i}{Environment.NewLine}"));
                instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality = weapon.maxQuality; // Sets the max level an item can be upgraded to.
                ZLog.Log($"Updated {instanceMRecipe.m_item.name} of {instanceMRecipe.name}, set m_maxQuality to {instanceMRecipe.m_item.m_itemData.m_shared.m_maxQuality}");

                //ObjectDB.instance.m_items
                
                //instanceMRecipe.m_item.m_itemData.m_shared.

                //ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Bow, startWith:"XBow");
                //ItemDrop.ItemData.SharedData mShared//

                //stanceMRecipe.m_item.m_itemData.

                //ItemDrop.ItemData item;
                //item.m_shared.

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
                //instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields().Select(field => field.Name.ToList());
                //ZLog.Log($"{instanceMRecipe.m_item.m_itemData.m_shared.GetType()}");
                //ZLog.Log($"{instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields().Select(field => field.Name.ToList())}");

                BindingFlags bindingFlags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Static;

                /*foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields(bindingFlags))
                {
                    ZLog.Log($"{field.Name.ToString()}");
                }*/

                foreach (string effects in weapon.sharedStats)
                {
                    //ZLog.Log($"{field.Name.ToString()}");
                    //ZLog.Log($"{effects}");
                    foreach (FieldInfo field in instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields(bindingFlags))
                    {

                        
                        string[] parsedEffects = effects.Split(':');
                        //ZLog.Log($"{field.Name.ToString()}");
                        string affixedName = parsedEffects.GetValue(0).ToString();
                        if(parsedEffects.GetValue(0).ToString().StartsWith("m_") == false)
                        {
                            affixedName = "m_" + affixedName;
                        }
                        /*ZLog.Log($"{affixedName}");
                        ZLog.Log($"{parsedEffects.GetValue(0).ToString()}{Environment.NewLine}");
                        ZLog.Log($"{field.Name.ToString()}{Environment.NewLine}");*/
                        /*if (parsedEffects.GetValue(0).ToString().Equals(field.Name.ToString()))
                        {
                            ZLog.Log($"Inside Loop 1: {parsedEffects.GetValue(0)}");
                            ZLog.Log($"Inside Loop 1: {field.Name.ToString()}");
                        }*/
                        if (affixedName.ToString().Equals(field.Name.ToString()))
                        {
                            field.SetValue(instanceMRecipe.m_item.m_itemData.m_shared, parsedEffects.GetValue(1));
                            if(affixedName == "m_attackForce")
                            {
                                ZLog.Log($"{instanceMRecipe.m_item.m_itemData.m_shared.m_attackForce}{Environment.NewLine}");
                            }
                            //ZLog.Log($"Inside Loop 2: {affixedName}");
                            //ZLog.Log($"Inside Loop 2: {field.Name.ToString()}");

                            //instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields().Where(r => r.Name == affixedName)
                        }
                    }
                }
                

                /*var test = instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields().Select(field => field.Name.ToList());

                foreach(var s in instanceMRecipe.m_item.m_itemData.m_shared.GetType().GetFields().Select(field => field.Name.ToList()))
                {
                    ZLog.Log($"{s.ToString()}{Environment.NewLine}");
                }*/

                /*foreach (string effects in weapon.effectsStats)
                {
                    string[] parsedEffects = effects.Split(':');
                    ZLog.Log($"{requirement.m_resItem.name}{Environment.NewLine}");
                    if (parsedEffects.GetValue(0).Equals(instanceMRecipe.m_item.m_itemData.m_shared.GetType()))
                    {
                        requirement.m_amountPerLevel = Convert.ToInt32(parsedEffects.GetValue(1));
                        ZLog.Log($"The item we want to modify: {parsedEffects.GetValue(0)}{Environment.NewLine}");
                        ZLog.Log($"Material modifed: {requirement.m_resItem.name}{Environment.NewLine}");
                        ZLog.Log($"New material requiredment: {requirement.m_amountPerLevel}{Environment.NewLine}");
                    }
                }*/
            }
        }
    }
}
