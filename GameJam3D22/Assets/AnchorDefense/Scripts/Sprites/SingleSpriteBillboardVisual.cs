using UnityEngine;

namespace AnchorDefense
{
    public sealed class SingleSpriteBillboardVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private bool flipHorizontally = true;
        [SerializeField] private bool sourceSpriteFacesRight = true;
        [SerializeField, Min(0f)] private float flipDeadZone = 0.03f;

        private Camera targetCamera;
        private Vector3 worldDirection = Vector3.down;

        public SpriteRenderer TargetRenderer => targetRenderer;

        public void Configure(SpriteRenderer renderer, bool allowHorizontalFlip = true)
        {
            targetRenderer = renderer;
            flipHorizontally = allowHorizontalFlip;
        }

        public void SetWorldDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                worldDirection = direction.normalized;
            }
        }

        private void LateUpdate()
        {
            targetCamera = targetCamera != null ? targetCamera : Camera.main;
            if (targetCamera == null || targetRenderer == null)
            {
                return;
            }

            transform.rotation = targetCamera.transform.rotation;
            if (!flipHorizontally)
            {
                return;
            }

            float horizontalDirection = Vector3.Dot(worldDirection, targetCamera.transform.right);
            if (Mathf.Abs(horizontalDirection) > flipDeadZone)
            {
                bool movingLeftOnScreen = horizontalDirection < 0f;
                targetRenderer.flipX = sourceSpriteFacesRight ? movingLeftOnScreen : !movingLeftOnScreen;
            }
        }
    }
}
