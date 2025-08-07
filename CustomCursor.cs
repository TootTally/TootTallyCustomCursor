using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using TootTallyCore.Utils.TootTallyGlobals;

namespace TootTallyCustomCursor
{
    public static class CustomCursor
    {
        private const string NOTETARGET_PATH = "GameplayCanvas/GameSpace/TargetNote";
        private const string NOTEDOT_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot";
        private const string NOTEDOTGLOW_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot-glow";
        private const string NOTEDOTGLOW1_PATH = "GameplayCanvas/GameSpace/TargetNote/note-dot-glow/note-dot-glow (1)";

        private static Texture2D _noteTargetTexture, _noteDotTexture, _noteDotGlowTexture, _noteDotGlow1Texture;
        private static Texture2D _trailTexture;
        private static string _lastCursorName;

        public static Texture2D[] GetTextures => new Texture2D[] { _noteTargetTexture, _noteDotTexture, _trailTexture };

        public static Action<GameController> OnTextureLoaded = OnAllTextureLoadedAfterConfigChange;

        public static void LoadCursorTexture(GameController __instance, string cursorName)
        {
            //If textures are already set, skip
            if (AreAllTexturesLoaded() && !ConfigCursorNameChanged()) return;

            var cursorPath = Path.Combine(Paths.BepInExRootPath, Plugin.CURSORS_FOLDER_PATH, cursorName);

            //Dont know which will request will finish first...
            Plugin.Instance.StartCoroutine(LoadCursorTexture(cursorPath + "/TargetNote.png", texture =>
            {
                _noteTargetTexture = texture;
                Plugin.LogInfo("Target Texture Loaded.");
                if (AreAllTexturesLoaded())
                    OnTextureLoaded.Invoke(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(cursorPath + "/note-dot.png", texture =>
            {
                _noteDotTexture = texture;
                Plugin.LogInfo("Dot Texture Loaded.");
                if (AreAllTexturesLoaded())
                    OnTextureLoaded.Invoke(__instance);

            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(cursorPath + "/note-dot-glow.png", texture =>
            {
                _noteDotGlowTexture = texture;
                Plugin.LogInfo("Dot Glow Texture Loaded.");
                if (AreAllTexturesLoaded())
                    OnTextureLoaded.Invoke(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(cursorPath + "/note-dot-glow (1).png", texture =>
            {
                _noteDotGlow1Texture = texture;
                Plugin.LogInfo("Dot Glow1 Texture Loaded.");
                if (AreAllTexturesLoaded())
                    OnTextureLoaded.Invoke(__instance);
            }));
            Plugin.Instance.StartCoroutine(LoadCursorTexture(cursorPath + "/trail.png", texture =>
            {
                _trailTexture = texture;
                Plugin.LogInfo("Trail Texture Loaded.");
                if (AreAllTexturesLoaded())
                    OnTextureLoaded.Invoke(__instance);
            }));
        }

        public static void UnloadTextures()
        {
            GameObject.DestroyImmediate(_noteTargetTexture);
            GameObject.DestroyImmediate(_noteDotTexture);
            GameObject.DestroyImmediate(_noteDotGlowTexture);
            GameObject.DestroyImmediate(_noteDotGlow1Texture);
            GameObject.DestroyImmediate(_trailTexture);
            Plugin.LogInfo("Custom Cursor Textures Destroyed.");
        }

        public static void OnAllTextureLoadedAfterConfigChange(GameController __instance)
        {
            if (__instance == null || !__instance.pointer.activeSelf) return;
            ApplyCustomTextureToCursor(__instance);
            _lastCursorName = Plugin.Instance.CursorName.Value;

        }

        public static bool AreAllTexturesLoaded() => _noteTargetTexture != null && _noteDotTexture != null && _noteDotGlowTexture != null && _noteDotGlow1Texture != null && _trailTexture != null;

        public static bool CanApplyTextures() => _noteTargetTexture != null || _noteDotTexture != null || _noteDotGlowTexture != null || _noteDotGlow1Texture != null || _trailTexture != null;

        public static bool ConfigCursorNameChanged() => Plugin.Instance.CursorName.Value != _lastCursorName;

        public static IEnumerator<UnityWebRequestAsyncOperation> LoadCursorTexture(string filePath, Action<Texture2D> callback)
        {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(filePath);
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError && !webRequest.isHttpError)
                callback(DownloadHandlerTexture.GetContent(webRequest));
            else
                Plugin.LogInfo($"{Path.GetFileName(filePath)} does not exist or have the wrong format.");
        }

        public static void ApplyCustomTextureToCursor(GameController __instance)
        {
            if (!CanApplyTextures()) return;

            Plugin.LogInfo("Applying Custom Textures to cursor.");

            GameObject noteTarget = GameObject.Find(NOTETARGET_PATH).gameObject;
            noteTarget.transform.localScale = Vector3.one * Plugin.Instance.CursorSize.Value;

            if (_noteTargetTexture != null)
            {
                var targetImage = noteTarget.GetComponent<Image>();
                targetImage.sprite = Sprite.Create(_noteTargetTexture, new Rect(0, 0, _noteTargetTexture.width, _noteTargetTexture.height), Vector2.one);
                targetImage.color = Plugin.Instance.CursorColor.Value;
                noteTarget.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteTargetTexture.width, _noteTargetTexture.height) / 2;
            }
            if (_noteDotTexture != null)
            {
                GameObject noteDot = GameObject.Find(NOTEDOT_PATH).gameObject;
                var dotImage = noteDot.GetComponent<Image>();
                dotImage.sprite = Sprite.Create(_noteDotTexture, new Rect(0, 0, _noteDotTexture.width, _noteDotTexture.height), Vector2.zero);
                dotImage.color = Plugin.Instance.CursorColor.Value;
                noteDot.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotTexture.width, _noteDotTexture.height) / 2;
            }
            if (_noteDotGlowTexture != null)
            {
                GameObject noteDotGlow = GameObject.Find(NOTEDOTGLOW_PATH).gameObject;
                var dotGlowImage = noteDotGlow.GetComponent<Image>();
                dotGlowImage.sprite = Sprite.Create(_noteDotGlowTexture, new Rect(0, 0, _noteDotGlowTexture.width, _noteDotGlowTexture.height), Vector2.zero);
                //dotGlowImage.color = Plugin.Instance.CursorColor.Value;
                noteDotGlow.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotGlowTexture.width, _noteDotGlowTexture.height) / 2;
            }
            if (_noteDotGlow1Texture != null)
            {
                GameObject noteDotGlow1 = GameObject.Find(NOTEDOTGLOW1_PATH).gameObject;
                var dotGlow1Image = noteDotGlow1.GetComponent<Image>();
                dotGlow1Image.sprite = Sprite.Create(_noteDotGlow1Texture, new Rect(0, 0, _noteDotGlow1Texture.width, _noteDotGlow1Texture.height), Vector2.zero);
                //dotGlow1Image.color = Plugin.Instance.CursorColor.Value;
                noteDotGlow1.GetComponent<RectTransform>().sizeDelta = new Vector2(_noteDotGlow1Texture.width, _noteDotGlow1Texture.height) / 2;
            }
        }

        public static void AddTrail(GameController __instance)
        {
            if (!__instance.pointer.activeSelf) return;

            float scalerModifier = 1f;
            float trailSpeed = Plugin.Instance.TrailSpeed.Value;
            if (Plugin.Instance.TrailAdjustTrailSpeed.Value) //If this isn't the trail preview and trail has to be auto adjusted
            {
                float aspectRatioMult = GlobalVariables.testScreenRatio() == 1610 ? .9f : 1f; //_StaticR-atl - Account for scroll speed being affected by aspect ratio.
                scalerModifier = __instance.tempo * TootTallyGlobalVariables.gameSpeedMultiplier * __instance.defaultnotelength / 40600f * aspectRatioMult;
                trailSpeed = 17f * scalerModifier; //Hard coded 17f trail speed
            }


            if (_trailTexture != null)
            {
                __instance.pointer.AddComponent<CursorTrail>().Init(
                   _trailTexture.height * Plugin.Instance.TrailSize.Value,
                   Plugin.Instance.TrailLength.Value / scalerModifier, //_StaticR-atl - Keep trail length the same.
                   trailSpeed,
                   Plugin.Instance.TrailStartColor.Value,
                   Plugin.Instance.TrailEndColor.Value,
                   __instance.notelinesholder.transform.GetChild(0).GetComponent<LineRenderer>().material,
                   3500,
                   (int)Plugin.Instance.TrailRefreshRate.Value,
                   _trailTexture);
            }
            else //Means you're using default cursor
            {
                var trailPath = Path.Combine(Paths.BepInExRootPath, Plugin.CURSORS_FOLDER_PATH, "TEMPLATE/trail.png");
                Plugin.Instance.StartCoroutine(LoadCursorTexture(trailPath, texture =>
                {
                    _trailTexture = texture;
                    __instance.pointer.AddComponent<CursorTrail>().Init(
                   _trailTexture.height * Plugin.Instance.TrailSize.Value,
                   Plugin.Instance.TrailLength.Value / scalerModifier,
                   trailSpeed,
                   Plugin.Instance.TrailStartColor.Value,
                   Plugin.Instance.TrailEndColor.Value,
                   __instance.notelinesholder.transform.GetChild(0).GetComponent<LineRenderer>().material,
                   3500,
                   (int)Plugin.Instance.TrailRefreshRate.Value,
                   _trailTexture);
                }));
            }

        }

        public static void ResolvePresets(GameController __instance)
        {
            if ((!AreAllTexturesLoaded() || __instance == null) && Plugin.Instance.CursorName.Value != Plugin.DEFAULT_CURSORNAME)
            {
                Plugin.LogInfo($"[{Plugin.Instance.CursorName.Value}] preset loading...");
                LoadCursorTexture(__instance, Plugin.Instance.CursorName.Value);
            }
            else if (Plugin.Instance.CursorName.Value != Plugin.DEFAULT_CURSORNAME)
                ApplyCustomTextureToCursor(__instance);
            else
            {
                UnloadTextures();
                OnTextureLoaded.Invoke(__instance);
                Plugin.LogInfo("[Default] preset selected. Not loading any Custom Cursor.");
            }
        }
    }
}
