using UnityEngine;

namespace AnchorDefense
{
    public sealed class SlowSelfRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 6f;

        public Vector3 LocalAxis => localAxis;
        public float DegreesPerSecond => degreesPerSecond;

        private void Update()
        {
            Vector3 axis = localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.up;
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
