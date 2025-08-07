using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.Helpers;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using TootTallySettings.TootTallySettingsObjects;
using UnityEngine;
using static UnityEngine.UI.Dropdown;

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
            CursorSize = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorSize), 1f);
            CursorTrailEnabled = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorTrailEnabled), false);
            CursorColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorColor), Color.white, "Color hue shift for the cursor.");
            TrailAdjustTrailSpeed = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailAdjustTrailSpeed), false, "Automatically adjust the speed of the trail to match the scroll speed.");
            TrailSize = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSize), .5f);
            TrailLength = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailLength), .1f);
            TrailSpeed = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSpeed), 15f);
            TrailStartColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailStartColor), Color.white);
            TrailEndColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailEndColor), Color.white);
            TrailRefreshRate = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailRefreshRate), 0f, "Set to 0 for uncapped.");

            string sourceFolderPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "CustomCursors");
            string targetFolderPath = Path.Combine(Paths.BepInExRootPath, "CustomCursors");
            FileHelper.TryMigrateFolder(sourceFolderPath, targetFolderPath, true);

            settingPage = TootTallySettingsManager.AddNewPage(new CustomCursorSettingPage());
            TootTallySettings.Plugin.TryAddThunderstoreIconToPageButton(Instance.Info.Location, Name, settingPage);
            CustomCursor.ResolvePresets(null);

            _harmony.PatchAll(typeof(CustomCursorPatches));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            CustomCursor.UnloadTextures();
            LogInfo($"Module unloaded!");
        }

        public static class CustomCursorPatches
        {
            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPostfix]
            public static void OnHomeStartLoadTexture(HomeController __instance)
            {
                //Holy shit AHAH
                (settingPage as CustomCursorSettingPage).defaultCursor = __instance.testing_zone_mouse_text.transform.parent.parent.Find("GameSpace/TargetNote").gameObject;
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomCursor.ResolvePresets(__instance);

                if (Plugin.Instance.CursorTrailEnabled.Value)
                    CustomCursor.AddTrail(__instance);
            }
        }

        public ConfigEntry<string> CursorName { get; set; }
        public ConfigEntry<float> CursorSize { get; set; }
        public ConfigEntry<bool> CursorTrailEnabled { get; set; }
        public ConfigEntry<Color> CursorColor { get; set; }
        public ConfigEntry<bool> TrailAdjustTrailSpeed { get; set; }
        public ConfigEntry<float> TrailSize { get; set; }
        public ConfigEntry<float> TrailLength { get; set; }
        public ConfigEntry<float> TrailSpeed { get; set; }
        public ConfigEntry<float> TrailRefreshRate { get; set; }
        public ConfigEntry<Color> TrailStartColor { get; set; }
        public ConfigEntry<Color> TrailEndColor { get; set; }
    }
}