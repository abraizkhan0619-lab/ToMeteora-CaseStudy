using UnityEngine;

namespace ToMeteora
{
    public class PlayerStart : MonoBehaviour
    {
        [Tooltip("Height of the marker drawn in the Scene view.")]
        public float markerHeight = 2.5f;

        void OnDrawGizmos()
        {
            var position = transform.position;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(position + Vector3.up * 0.4f, 0.4f);
            Gizmos.DrawWireSphere(position + Vector3.up * (markerHeight - 0.4f), 0.4f);
            Gizmos.DrawLine(position + new Vector3(0.4f, 0.4f, 0f), position + new Vector3(0.4f, markerHeight - 0.4f, 0f));
            Gizmos.DrawLine(position + new Vector3(-0.4f, 0.4f, 0f), position + new Vector3(-0.4f, markerHeight - 0.4f, 0f));

            var forward = transform.forward;
            var tip = position + Vector3.up * 0.05f + forward * 1.5f;
            Gizmos.DrawLine(position + Vector3.up * 0.05f, tip);
            Gizmos.DrawLine(tip, tip - forward * 0.4f + transform.right * 0.25f);
            Gizmos.DrawLine(tip, tip - forward * 0.4f - transform.right * 0.25f);
        }
    }
}
