using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    [RequireComponent(typeof(Selectable))]
    public sealed class UiHoverSoundPlayer : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0f)] private float minimumSecondsBetweenPlays = 0.15f;

        private static float lastHoverSoundRealtime = float.NegativeInfinity;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
            if (audioSource == null || hoverClip == null)
            {
                return;
            }
            if (!ShouldPlayHoverSound(Time.unscaledTime, minimumSecondsBetweenPlays))
            {
                return;
            }

            audioSource.PlayOneShot(hoverClip, volume);
        }

        public static bool ShouldPlayHoverSound(float realtime, float minimumSecondsBetweenPlays)
        {
            float cooldown = Mathf.Max(0f, minimumSecondsBetweenPlays);
            if (realtime - lastHoverSoundRealtime < cooldown)
            {
                return false;
            }

            lastHoverSoundRealtime = realtime;
            return true;
        }

        public static void ResetHoverSoundCooldown()
        {
            lastHoverSoundRealtime = float.NegativeInfinity;
        }

#if UNITY_EDITOR
public void Configure(AudioSource source, AudioClip clip, float clipVolume)
        {
            audioSource = source;
            hoverClip = clip;
            volume = Mathf.Clamp01(clipVolume);
            minimumSecondsBetweenPlays = 0.15f;
        }
#endif
    }
}
