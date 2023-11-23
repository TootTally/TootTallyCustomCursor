using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyCustomCursor
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallyTournamentHost", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "CustomCursor.cfg";
        private const string CURSOR_CONFIG_FIELD = "CustomCursor";
        public const string DEFAULT_CURSORNAME = "Default";
        public static string CURSORS_FOLDER_PATH = "CustomCursors";

        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => "CustomCursor"; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);


        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "Custom Cursor", true, "Enable Custom Cursor Module");
            TootTallyModuleManager.AddModule(this);
            TootTallySettings.Plugin.Instance.AddModuleToSettingPage(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true) { SaveOnConfigSet = true };
            CursorName = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorName), DEFAULT_CURSORNAME);
            CursorTrailName = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorTrailName), false);
            TrailSize = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSize), .5f);
            TrailLength = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailLength), .1f);
            TrailSpeed = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSpeed), 15f);
            TrailStartColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailStartColor), Color.white);
            TrailEndColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailEndColor), Color.white);

            TryMigrateFolder("CustomCursors");

            settingPage = TootTallySettingsManager.AddNewPage("Custom Cursor", "Custom Cursor", 40f, new Color(0, 0, 0, 0));
            CreateDropdownFromFolder(CURSORS_FOLDER_PATH, CursorName, DEFAULT_CURSORNAME);
            settingPage.AddLabel("CustomTrailLabel", "Custom Trail", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddToggle("CursorTrail", CursorTrailName);
            settingPage.AddSlider("Trail Size", 0, 1, TrailSize, false);
            settingPage.AddSlider("Trail Length", 0, 1, TrailLength, false);
            settingPage.AddSlider("Trail Speed", 0, 100, TrailSpeed, false);
            settingPage.AddLabel("Trail Start Color");
            settingPage.AddColorSliders("Trail Start Color", "Trail Start Color", TrailStartColor);
            settingPage.AddLabel("Trail End Color");
            settingPage.AddColorSliders("Trail End Color", "Trail End Color", TrailEndColor);

            _harmony.PatchAll(typeof(CustomCursorPatches));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public void TryMigrateFolder(string folderName)
        {
            string targetFolderPath = Path.Combine(Paths.BepInExRootPath, folderName);
            if (!Directory.Exists(targetFolderPath))
            {
                string sourceFolderPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), folderName);
                LogInfo($"{folderName} folder not found. Attempting to move folder from " + sourceFolderPath + " to " + targetFolderPath);
                if (Directory.Exists(sourceFolderPath))
                    Directory.Move(sourceFolderPath, targetFolderPath);
                else
                {
                    LogError($"Source {folderName} Folder Not Found. Cannot Create {folderName} Folder. Download the module again to fix the issue.");
                    return;
                }
            }
        }

        public void CreateDropdownFromFolder(string folderName, ConfigEntry<string> config, string defaultValue)
        {
            var folderNames = new List<string> { defaultValue };
            var folderPath = Path.Combine(Paths.BepInExRootPath, folderName);
            if (Directory.Exists(folderPath))
            {
                var directories = Directory.GetDirectories(folderPath).ToList();
                directories.ForEach(d =>
                {
                    if (!d.Contains("TEMPLATE"))
                        folderNames.Add(Path.GetFileNameWithoutExtension(d));
                });
            }
            settingPage.AddLabel(folderName, folderName, 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddDropdown($"{folderName}Dropdown", config, folderNames.ToArray());
        }

        public static class CustomCursorPatches
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.tryToSaveSettings))]
            [HarmonyPostfix]
            public static void OnSettingsChange()
            {
                CustomCursor.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeStartLoadTexture()
            {
                CustomCursor.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomCursor.ResolvePresets(__instance);
            }
        }

        public ConfigEntry<string> CursorName { get; set; }
        public ConfigEntry<bool> CursorTrailName { get; set; }
        public ConfigEntry<float> TrailSize { get; set; }
        public ConfigEntry<float> TrailLength { get; set; }
        public ConfigEntry<float> TrailSpeed { get; set; }
        public ConfigEntry<Color> TrailStartColor { get; set; }
        public ConfigEntry<Color> TrailEndColor { get; set; }
    }
}