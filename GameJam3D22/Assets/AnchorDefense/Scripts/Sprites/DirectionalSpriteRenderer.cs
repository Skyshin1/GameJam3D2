using UnityEngine;

namespace AnchorDefense
{
    public sealed class DirectionalSpriteRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private DirectionalSpriteSet spriteSet;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private float directionAngleOffset;

        private Camera targetCamera;
        private Vector3 worldDirection = Vector3.up;
        private int currentDirectionIndex = -1;

        public void Configure(SpriteRenderer renderer, DirectionalSpriteSet set, bool billboard = true)
        {
            targetRenderer = renderer;
            spriteSet = set;
            faceCamera = billboard;
            currentDirectionIndex = -1;
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
            if (targetCamera == null || targetRenderer == null || spriteSet == null)
            {
                return;
            }

            Vector2 cameraPlaneDirection = new Vector2(
                Vector3.Dot(worldDirection, targetCamera.transform.right),
                Vector3.Dot(worldDirection, targetCamera.transform.up));

            if (cameraPlaneDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(cameraPlaneDirection.x, cameraPlaneDirection.y) * Mathf.Rad2Deg;
                angle += directionAngleOffset;
                int directionIndex = Mathf.RoundToInt(angle / 45f);
                directionIndex = (directionIndex % DirectionalSpriteSet.DirectionCount + DirectionalSpriteSet.DirectionCount) % DirectionalSpriteSet.DirectionCount;

                if (directionIndex != currentDirectionIndex)
                {
                    currentDirectionIndex = directionIndex;
                    targetRenderer.sprite = spriteSet.GetSprite(directionIndex);
                }
            }

            if (faceCamera)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
    }
}
