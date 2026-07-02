using UnityEngine;
using UnityEngine.EventSystems;

namespace AnchorDefense
{
    public sealed class RingInputController : MonoBehaviour
    {
        [SerializeField] private float rotationSensitivity = 0.32f;

        private Camera gameplayCamera;
        private OrbitRingController selectedRing;
        private Vector3 previousMousePosition;

        public void Initialize(Camera targetCamera)
        {
            gameplayCamera = targetCamera;
        }

        private void Update()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                SelectRingUnderPointer();
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0) && selectedRing != null)
            {
                Vector3 currentPosition = Input.mousePosition;
                float delta = currentPosition.x - previousMousePosition.x;
                selectedRing.RotateByDrag(delta, rotationSensitivity);
                previousMousePosition = currentPosition;
            }
        }

        private void SelectRingUnderPointer()
        {
            Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
            OrbitRingController nextSelection = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                nextSelection = hit.collider.GetComponentInParent<OrbitRingController>();
            }

            if (selectedRing == nextSelection)
            {
                return;
            }

            selectedRing?.SetSelected(false);
            selectedRing = nextSelection;
            selectedRing?.SetSelected(true);
        }
    }
}
