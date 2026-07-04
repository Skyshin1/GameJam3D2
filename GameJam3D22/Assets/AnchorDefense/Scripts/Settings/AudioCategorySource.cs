using UnityEngine;

namespace AnchorDefense
{
    public enum AudioCategory
    {
        Music,
        SoundEffect
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioCategorySource : MonoBehaviour
    {
        [SerializeField] private AudioCategory category;
        [SerializeField, Range(0f, 1f)] private float sourceVolume = 1f;
        private AudioSource audioSource;

        private void OnEnable()
        {
            audioSource = GetComponent<AudioSource>();
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
            float categoryVolume = category == AudioCategory.Music
                ? settings.musicVolume
                : settings.soundEffectsVolume;
            audioSource.volume = sourceVolume * categoryVolume;
        }
    }
}
