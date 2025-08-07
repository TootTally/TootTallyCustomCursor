﻿using BepInEx.Configuration;
using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallySettings;
using TootTallySettings.TootTallySettingsObjects;
using UnityEngine;
using UnityEngine.UI;
using TootTallyCore.Graphics.Animations;
using TMPro;
using UnityEngine.PostProcessing;

namespace TootTallyCustomCursor
{
    internal class CustomCursorSettingPage : TootTallySettingPage
    {
        private static ColorBlock _pageBtnColors = new ColorBlock()
        {
            colorMultiplier = 1f,
            fadeDuration = .2f,
            disabledColor = Color.gray,
            normalColor = new Color(1, 0, 0),
            pressedColor = new Color(1f, .2f, .2f),
            highlightedColor = new Color(.8f, 0, 0),
            selectedColor = new Color(1, 0, 0)
        };

        private TootTallySettingDropdown _cursorDropdown;
        private TootTallySettingSlider _cursorSizeSlider, _trailSizeSlider, _trailLengthSlider, _trailSpeedSlider, _trailRefreshRateSlider;
        private TootTallySettingColorSliders _cursorColorSliders, _trailStartColorSliders, _trailEndColorSliders;
        private TootTallySettingToggle _trailToggle, _trailAutoadjustToggle;

        public CustomCursorSettingPage() : base("Custom Cursor", "Custom Cursor", 40f, new Color(0, 0, 0, 0), _pageBtnColors)
        {
            _cursorDropdown = CreateDropdownFromFolder(Plugin.CURSORS_FOLDER_PATH, Plugin.Instance.CursorName, Plugin.DEFAULT_CURSORNAME);
            _cursorSizeSlider = AddSlider("Cursor Size", .01f, 2f, Plugin.Instance.CursorSize, false);
            AddLabel("CustomTrailLabel", "Custom Trail", 24, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
            AddLabel("Cursor Color Shift");
            _cursorColorSliders = AddColorSliders("Cursor Color", "Cursor Color", Plugin.Instance.CursorColor);
            _trailToggle = AddToggle("Enable Cursor Trail", Plugin.Instance.CursorTrailEnabled);
            _trailAutoadjustToggle = AddToggle("Auto adjust Trail speed", Plugin.Instance.TrailAdjustTrailSpeed);
            _trailSizeSlider = AddSlider("Trail Size", 0, 1, Plugin.Instance.TrailSize, false);
            _trailLengthSlider = AddSlider("Trail Length", 0, 1, Plugin.Instance.TrailLength, false);
            _trailSpeedSlider = AddSlider("Trail Speed", 0, 100, Plugin.Instance.TrailSpeed, false);
            _trailRefreshRateSlider = AddSlider("Trail Refresh Rate", 0, 200, Plugin.Instance.TrailRefreshRate, true);
            AddLabel("Trail Start Color");
            _trailStartColorSliders = AddColorSliders("Trail Start Color", "Trail Start Color", Plugin.Instance.TrailStartColor);
            AddLabel("Trail End Color");
            _trailEndColorSliders = AddColorSliders("Trail End Color", "Trail End Color", Plugin.Instance.TrailEndColor);
        }

        public override void Initialize()
        {
            base.Initialize();
            _cursorDropdown.dropdown.onValueChanged.AddListener(value =>
            {
                CustomCursor.ResolvePresets(null);
            });
            _cursorColorSliders.sliderR.onValueChanged.AddListener(UpdateColor);
            _cursorColorSliders.sliderG.onValueChanged.AddListener(UpdateColor);
            _cursorColorSliders.sliderB.onValueChanged.AddListener(UpdateColor);
            _trailToggle.toggle.onValueChanged.AddListener(OnTrailToggle);
            _trailAutoadjustToggle.toggle.onValueChanged.AddListener(OnAutoResizeValueUpdate);
            _cursorSizeSlider.slider.onValueChanged.AddListener(OnCursorSizeChange);
            _trailStartColorSliders.sliderR.onValueChanged.AddListener(UpdateColor);
            _trailStartColorSliders.sliderG.onValueChanged.AddListener(UpdateColor);
            _trailStartColorSliders.sliderB.onValueChanged.AddListener(UpdateColor);
            _trailEndColorSliders.sliderR.onValueChanged.AddListener(UpdateColor);
            _trailEndColorSliders.sliderG.onValueChanged.AddListener(UpdateColor);
            _trailEndColorSliders.sliderB.onValueChanged.AddListener(UpdateColor);
            _trailLengthSlider.slider.onValueChanged.AddListener(value => _trailPreview?.SetLifetime(value));
            _trailSpeedSlider.slider.onValueChanged.AddListener(value => _trailPreview?.SetTrailSpeed(value * 3.4f));
            _trailSizeSlider.slider.onValueChanged.AddListener(value => _trailPreview?.SetWidth(value * _trailTexture.height * 2f));
            _trailRefreshRateSlider.slider.onValueChanged.AddListener(value => _trailPreview?.SetRefreshRate((int)value));
            OnAutoResizeValueUpdate(Plugin.Instance.TrailAdjustTrailSpeed.Value);
        }

        private void OnAutoResizeValueUpdate(bool value)
        {
            _trailSpeedSlider.slider.gameObject.SetActive(!value);
            _trailPreview?.SetTrailSpeed((value ? 17f : _trailSpeedSlider.slider.value) * 3.4f);
        }

        private void OnCursorSizeChange(float value)
        {
            _cursorPreview.transform.localScale = Vector3.one * value;
        }

        private void UpdateColor(float f)
        {
            if (_cursorPreviewTargetImage != null)
                _cursorPreviewTargetImage.color = Plugin.Instance.CursorColor.Value;
            if (_cursorPreviewDotImage != null)
                _cursorPreviewDotImage.color = Plugin.Instance.CursorColor.Value;

            _trailPreview?.UpdateColors();
        }

        public GameObject defaultCursor;

        private GameObject _cursorPreview;
        private Image _cursorPreviewTargetImage, _cursorPreviewDotImage;
        private TootTallyAnimation _previewAnimation;
        private CursorTrail _trailPreview;
        private static Texture2D[] _textures;
        private static Texture2D _trailTexture;

        public override void OnShow()
        {
            CustomCursor.OnTextureLoaded += InitPreview;
            base.OnShow();
            InitPreview(null);
        }

        public override void OnHide()
        {
            CustomCursor.OnTextureLoaded -= InitPreview;
            base.OnHide();
            DestroyPreview();
        }

        private void InitPreview(GameController _instance)
        {
            if (_cursorPreview != null)
                DestroyPreview();

            _cursorPreview = GameObject.Instantiate(defaultCursor, gridPanel.transform.parent);
            _cursorPreview.transform.localScale = Vector3.one * Plugin.Instance.CursorSize.Value;
            GameObject noteDot = _cursorPreview.transform.Find("note-dot").gameObject;

            _cursorPreview.GetComponent<RectTransform>().anchoredPosition = new Vector2(950, 100);

            if (Plugin.Instance.CursorName.Value != Plugin.DEFAULT_CURSORNAME)
            {
                _textures = CustomCursor.GetTextures;
                _cursorPreviewTargetImage = _cursorPreview.GetComponent<Image>();
                _cursorPreviewTargetImage.sprite = Sprite.Create(_textures[0], new Rect(0, 0, _textures[0].width, _textures[0].height), Vector2.one);
                _cursorPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(_textures[0].width, _textures[0].height);
                _cursorPreview.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
                _cursorPreviewDotImage = noteDot.GetComponent<Image>();
                _cursorPreviewDotImage.sprite = Sprite.Create(_textures[1], new Rect(0, 0, _textures[1].width, _textures[1].height), Vector2.zero);
                noteDot.GetComponent<RectTransform>().sizeDelta = new Vector2(_textures[1].width, _textures[1].height);
            }
            else
            {
                _cursorPreviewTargetImage = _cursorPreview.GetComponent<Image>();
                var image = _cursorPreviewTargetImage.mainTexture as Texture2D;
                _cursorPreviewDotImage = noteDot.GetComponent<Image>();
                var image2 = _cursorPreviewDotImage.mainTexture as Texture2D;
                _cursorPreview.GetComponent<RectTransform>().sizeDelta = new Vector2(image.width, image.height);
                _cursorPreview.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
                noteDot.GetComponent<RectTransform>().sizeDelta = new Vector2(image2.width, image2.height);
            }


            _previewAnimation = TootTallyAnimationManager.AddNewPositionAnimation(_cursorPreview, new Vector2(950, 0), 99999f, new SecondDegreeDynamicsAnimation(.85f, 0, -1f));
            OnTrailToggle(Plugin.Instance.CursorTrailEnabled.Value);
        }

        private void DestroyPreview()
        {
            GameObject.DestroyImmediate(_cursorPreview);
            _previewAnimation?.Dispose();
        }

        private void OnTrailToggle(bool value)
        {
            if (_cursorPreview == null) return;

            if (_trailPreview == null && value)
            {
                _trailPreview = _cursorPreview.AddComponent<CursorTrail>();
                if (Plugin.Instance.CursorName.Value != Plugin.DEFAULT_CURSORNAME)
                {
                    _trailTexture = _textures[2];
                    _trailPreview.Init(
                  _trailTexture.height * Plugin.Instance.TrailSize.Value * 2f,
                  Plugin.Instance.TrailLength.Value,
                  Plugin.Instance.TrailSpeed.Value * 3.4f,
                  Plugin.Instance.TrailStartColor.Value,
                  Plugin.Instance.TrailEndColor.Value,
                  Material.GetDefaultLineMaterial(),
                  2500,
                  (int)Plugin.Instance.TrailRefreshRate.Value,
                  _trailTexture);
                }
                else
                {
                    var trailPath = Path.Combine(Paths.BepInExRootPath, Plugin.CURSORS_FOLDER_PATH, "TEMPLATE/trail.png");
                    Plugin.Instance.StartCoroutine(CustomCursor.LoadCursorTexture(trailPath, texture =>
                    {
                        _trailTexture = texture;
                        _trailPreview.Init(
                       _trailTexture.height * Plugin.Instance.TrailSize.Value * 2f,
                       Plugin.Instance.TrailLength.Value,
                       Plugin.Instance.TrailSpeed.Value * 3.4f,
                       Plugin.Instance.TrailStartColor.Value,
                       Plugin.Instance.TrailEndColor.Value,
                       Material.GetDefaultLineMaterial(),
                       2500,
                       (int)Plugin.Instance.TrailRefreshRate.Value,
                       _trailTexture);
                    }));
                }
            }
            else if (_trailPreview != null && !value)
            {
                _trailPreview.Dispose();
                _trailPreview = null;
            }
        }

        private TootTallySettingDropdown CreateDropdownFromFolder(string folderName, ConfigEntry<string> config, string defaultValue)
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
            AddLabel(folderName, folderName, 24, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.BottomLeft);
            return AddDropdown($"{folderName}Dropdown", config, folderNames.ToArray());
        }
    }
}
