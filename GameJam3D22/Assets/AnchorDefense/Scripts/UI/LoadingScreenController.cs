using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private RectTransform rotatingAnchor;
        [SerializeField, Min(0f)] private float minimumDisplayTime = 0.75f;

        public void Configure(Image fill, Text percentage, Text status, RectTransform anchor)
        {
            progressFill = fill;
            progressText = percentage;
            statusText = status;
            rotatingAnchor = anchor;
        }

        private void Update()
        {
            if (rotatingAnchor != null)
            {
                rotatingAnchor.Rotate(0f, 0f, -36f * Time.unscaledDeltaTime);
            }
        }

        private IEnumerator Start()
        {
            GameSettingsService.EnsureLoaded();
            string targetScene = string.IsNullOrEmpty(SceneLoadRequest.TargetScene)
                ? "Gameplay"
                : SceneLoadRequest.TargetScene;
            float startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float normalizedProgress = Mathf.Clamp01(operation.progress / 0.9f);
                progressFill.fillAmount = normalizedProgress;
                progressText.text = Mathf.RoundToInt(normalizedProgress * 100f) + "%";
                statusText.text = normalizedProgress < 0.9f ? "正在建立锚定连接…" : "锚定完成";

                bool minimumTimePassed = Time.realtimeSinceStartup - startedAt >= minimumDisplayTime;
                if (operation.progress >= 0.9f && minimumTimePassed)
                {
                    progressFill.fillAmount = 1f;
                    progressText.text = "100%";
                    operation.allowSceneActivation = true;
                }
                yield return null;
            }
        }
    }
}
