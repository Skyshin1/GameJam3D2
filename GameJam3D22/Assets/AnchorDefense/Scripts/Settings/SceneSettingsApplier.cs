using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnchorDefense
{
    public sealed class SceneSettingsApplier : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Volume brightnessVolume;

        public void Configure(Camera camera, Volume volume)
        {
            targetCamera = camera;
            brightnessVolume = volume;
        }

        private void OnEnable()
        {
            GameSettingsService.EnsureLoaded();
            GameSettingsService.Changed += Apply;
            Apply(GameSettingsService.Current);
        }

        private void OnDisable()
        {
            GameSettingsService.Changed -= Apply;
        }

        private void Apply(GameSettingsData settings)
        {
            ApplyBrightness(settings.brightness);
            ApplyAntiAliasing(settings.antiAliasing);
        }

        private void ApplyBrightness(float postExposure)
        {
            if (brightnessVolume == null)
            {
                return;
            }

            VolumeProfile profile = brightnessVolume.profile;
            if (!profile.TryGet(out ColorAdjustments adjustments))
            {
                adjustments = profile.Add<ColorAdjustments>(true);
            }
            adjustments.active = true;
            adjustments.postExposure.overrideState = true;
            adjustments.postExposure.value = postExposure;
        }

        private void ApplyAntiAliasing(AntiAliasingSetting setting)
        {
            if (targetCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = targetCamera.GetUniversalAdditionalCameraData();
            switch (setting)
            {
                case AntiAliasingSetting.Fxaa:
                    cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case AntiAliasingSetting.Smaa:
                    cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData.antialiasingQuality = AntialiasingQuality.High;
                    break;
                default:
                    cameraData.antialiasing = AntialiasingMode.None;
                    break;
            }
        }
    }
}
