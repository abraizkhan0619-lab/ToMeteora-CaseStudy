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

        public static IsometricCamera Active { get; private set; }

        Vector3 focus;
        Vector3 focusVelocity;
        float shakeStrength;
        float shakeDuration;
        float shakeTimeLeft;
        float shakeSeed;

        public float Distance => mapSize * 1.5f;

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        void Start() => focus = Target();

        void LateUpdate()
        {
            focus = Vector3.SmoothDamp(focus, Target(), ref focusVelocity, followSmoothTime);
            Apply(focus);
            ApplyShake();
        }

        public void Shake(float strength, float duration)
        {
            if (strength <= 0f || duration <= 0f)
                return;
            if (strength >= shakeStrength * (shakeTimeLeft / Mathf.Max(shakeDuration, 0.0001f)))
            {
                shakeStrength = strength;
                shakeDuration = duration;
                shakeTimeLeft = duration;
                shakeSeed = Random.value * 100f;
            }
        }

        void ApplyShake()
        {
            if (shakeTimeLeft <= 0f)
                return;

            shakeTimeLeft -= Time.deltaTime;
            float fade = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
            float time = Time.time * 30f;
            float x = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(shakeSeed + 37f, time) * 2f - 1f;
            transform.position += (transform.right * x + transform.up * y) * shakeStrength * fade * fade;
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
