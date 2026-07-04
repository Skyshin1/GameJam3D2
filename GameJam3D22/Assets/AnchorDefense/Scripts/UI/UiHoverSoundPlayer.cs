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

            audioSource.PlayOneShot(hoverClip, volume);
        }

#if UNITY_EDITOR
        public void Configure(AudioSource source, AudioClip clip, float clipVolume)
        {
            audioSource = source;
            hoverClip = clip;
            volume = Mathf.Clamp01(clipVolume);
        }
#endif
    }
}
