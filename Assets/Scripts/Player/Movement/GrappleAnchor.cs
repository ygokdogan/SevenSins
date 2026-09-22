using UnityEngine;

namespace Player.Movement
{
    public enum GrappleArrival { WallRun = 0, FlyThrough = 1 }

    [DisallowMultipleComponent]
    public sealed class GrappleAnchor : MonoBehaviour
    {
        [Tooltip("Optional exact attachment point. The raycast hit point is used when empty.")]
        [SerializeField] private Transform attachmentPoint;

        [Tooltip("FlyThrough anchors should use a trigger collider, leaving the flight path clear.")]
        [SerializeField] private GrappleArrival arrival = GrappleArrival.FlyThrough;
        public GrappleArrival Arrival => arrival;

        public Vector3 GetAttachmentPoint(Vector3 hitPoint) =>
            attachmentPoint != null ? attachmentPoint.position :
            arrival == GrappleArrival.FlyThrough ? transform.position : hitPoint;
    }
}
