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
            CursorTrailEnabled = config.Bind(CURSOR_CONFIG_FIELD, nameof(CursorTrailEnabled), false);
            TrailSize = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSize), .5f);
            TrailLength = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailLength), .1f);
            TrailSpeed = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailSpeed), 15f);
            TrailStartColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailStartColor), Color.white);
            TrailEndColor = config.Bind(CURSOR_CONFIG_FIELD, nameof(TrailEndColor), Color.white);

            string sourceFolderPath = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "CustomCursors");
            string targetFolderPath = Path.Combine(Paths.BepInExRootPath, "CustomCursors");
            FileHelper.TryMigrateFolder(sourceFolderPath, targetFolderPath, true);

            settingPage = TootTallySettingsManager.AddNewPage("Custom Cursor", "Custom Cursor", 40f, new Color(0, 0, 0, 0));
            CreateDropdownFromFolder(CURSORS_FOLDER_PATH, CursorName, DEFAULT_CURSORNAME);
            settingPage.AddLabel("CustomTrailLabel", "Custom Trail", 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            settingPage.AddToggle("Enable Cursor Trail", CursorTrailEnabled);
            settingPage.AddSlider("Trail Size", 0, 1, TrailSize, false);
            settingPage.AddSlider("Trail Length", 0, 1, TrailLength, false);
            settingPage.AddSlider("Trail Speed", 0, 100, TrailSpeed, false);
            settingPage.AddLabel("Trail Start Color");
            settingPage.AddColorSliders("Trail Start Color", "Trail Start Color", TrailStartColor);
            settingPage.AddLabel("Trail End Color");
            settingPage.AddColorSliders("Trail End Color", "Trail End Color", TrailEndColor);
            settingPage.OnShowEvent += OnShowEnableCursorPreview;
            settingPage.OnHideEvent += OnHideDisableCursorPreview;

            TootTallySettings.Plugin.TryAddThunderstoreIconToPageButton(Instance.Info.Location, Name, settingPage);

            _harmony.PatchAll(typeof(CustomCursorPatches));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.OnShowEvent -= OnShowEnableCursorPreview;
            settingPage.OnHideEvent -= OnHideDisableCursorPreview;
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        private static GameObject _cursorPreview;
        private static TootTallyAnimation _previewAnimation;
        private static Material _lineMaterial;

        private static void OnShowEnableCursorPreview(TootTallySettingPage page)
        {
            var textures = CustomCursor.GetTextures;
            if (textures[0] == null || textures[1] == null) return;

            _cursorPreview = GameObjectFactory.CreateImageHolder(page.gridPanel.transform.parent, new Vector2(80, 100), new Vector2(textures[0].width, textures[0].height),
                Sprite.Create(textures[0], new Rect(0, 0, textures[0].width, textures[0].height), Vector2.one / 2f), "yeah");
            _cursorPreview.transform.localScale = Vector2.one * 1.1f;
            GameObjectFactory.CreateImageHolder(_cursorPreview.transform, Vector2.zero, new Vector2(textures[1].width, textures[1].height),
                Sprite.Create(textures[1], new Rect(0, 0, textures[1].width, textures[1].height), Vector2.one / 2f), "yeahButInside");
            if (Plugin.Instance.CursorTrailEnabled.Value)
            {
                var trail = _cursorPreview.AddComponent<CursorTrail>();
                trail.Init(
                    textures[2].height * Plugin.Instance.TrailSize.Value * 2f,
                    Plugin.Instance.TrailLength.Value * 1.2f,
                    Plugin.Instance.TrailSpeed.Value * 1.2f,
                    Plugin.Instance.TrailStartColor.Value,
                    Plugin.Instance.TrailEndColor.Value,
                    _lineMaterial,
                    textures[2]);
                trail.SetupPreview();
                _previewAnimation = TootTallyAnimationManager.AddNewPositionAnimation(_cursorPreview, new Vector2(80, -20), 99999f, new SecondDegreeDynamicsAnimation(.85f, 0, -1f));
            }
                
        }

        private static void OnHideDisableCursorPreview(TootTallySettingPage page)
        {
            GameObject.DestroyImmediate(_cursorPreview);
            _previewAnimation?.Dispose();
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
            public static void OnHomeStartLoadTexture(HomeController __instance)
            {
                //Holy shit AHAH
                _lineMaterial = __instance.testing_zone_mouse_text.transform.parent.parent.Find("GameSpace/NoteLinesHolder").GetChild(0).GetComponent<LineRenderer>().material;
                CustomCursor.ResolvePresets(null);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void PatchCustorTexture(GameController __instance)
            {
                CustomCursor.ResolvePresets(__instance);

                if (Plugin.Instance.CursorTrailEnabled.Value)
                {
                    var cursorName = Plugin.Instance.CursorName.Value != Plugin.DEFAULT_CURSORNAME ? Plugin.Instance.CursorName.Value : "TEMPLATE";
                    CustomCursor.AddTrail(__instance);
                }
            }
        }

        public ConfigEntry<string> CursorName { get; set; }
        public ConfigEntry<bool> CursorTrailEnabled { get; set; }
        public ConfigEntry<float> TrailSize { get; set; }
        public ConfigEntry<float> TrailLength { get; set; }
        public ConfigEntry<float> TrailSpeed { get; set; }
        public ConfigEntry<Color> TrailStartColor { get; set; }
        public ConfigEntry<Color> TrailEndColor { get; set; }
    }
}