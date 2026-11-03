using UnityEngine;

namespace ToMeteora
{
    [RequireComponent(typeof(Camera))]
    public class IsometricCamera : MonoBehaviour
    {
        [Tooltip("The camera follows this demo's current character.")]
        public CharacterSelectDemo demo;
        [Tooltip("On: follow the character. Off: show the whole map.")]
        public bool followCharacter = true;
        [Tooltip("Half the visible height, in Unity units, while following.")]
        public float followZoom = 7f;
        [Tooltip("Seconds the camera takes to catch up.")]
        public float followSmoothTime = 0.25f;

        [Header("Map")]
        public Vector3 mapCenter = Vector3.zero;
        [Tooltip("Width of the square map in Unity units.")]
        public float mapSize = 40f;
        public float padding = 1f;
        [Tooltip("Room for trees and props sticking up from the ground.")]
        public float propHeight = 6f;

        [Header("Angle")]
        [Range(20f, 70f)] public float pitch = 35f;
        public float yaw = 45f;

        Vector3 focus;
        Vector3 focusVelocity;

        public float Distance => mapSize * 1.5f;

        void Start() => focus = Target();

        void LateUpdate()
        {
            focus = Vector3.SmoothDamp(focus, Target(), ref focusVelocity, followSmoothTime);
            Apply(focus);
        }

        public void Fit()
        {
            focus = Target();
            Apply(focus);
        }

        public void FocusOn(Vector3 point)
        {
            focus = point;
            Apply(point, followZoom);
        }

        bool IsFollowing => followCharacter && demo != null && demo.Current != null;

        Vector3 Target() => IsFollowing ? demo.Current.transform.position : mapCenter;

        void Apply(Vector3 lookAt) =>
            Apply(lookAt, IsFollowing ? followZoom : WholeMapSize(GetComponent<Camera>().aspect));

        void Apply(Vector3 lookAt, float size)
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = lookAt - transform.forward * Distance;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = Distance * 2f;
            cam.orthographicSize = size;
        }

        float WholeMapSize(float aspect)
        {
            float halfWidth = (mapSize + padding * 2f) * Mathf.Sqrt(2f) / 2f;
            float halfHeight = halfWidth * Mathf.Sin(pitch * Mathf.Deg2Rad) + propHeight * Mathf.Cos(pitch * Mathf.Deg2Rad) / 2f;
            return Mathf.Max(halfHeight, halfWidth / aspect);
        }
    }
}
