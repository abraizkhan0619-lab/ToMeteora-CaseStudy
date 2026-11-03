using System.Collections;
using UnityEngine;

namespace ToMeteora
{
    public class HeroCharacter : MonoBehaviour
    {
        public const string VisualChildName = "Visual";

        [Tooltip("Set by the setup tool.")]
        public CharacterDefinition definition;

        public bool IsReady => definition != null && transform.Find(VisualChildName) != null && GetComponent<AbilityBehaviour>() != null;

        public float CurrentHealth { get; private set; }
        public AbilityBehaviour Ability { get; private set; }
        public Animator Animator { get; private set; }

        [Tooltip("Area (x/z) the character must stay inside. Width or height 0 = no limit.")]
        public Rect moveBounds;

        const float Gravity = -20f;

        CharacterController controller;
        float verticalSpeed;
        float speedThisFrame;

        void Awake()
        {
            CurrentHealth = definition.health;
            Ability = GetComponent<AbilityBehaviour>();
            controller = GetComponent<CharacterController>();
            foreach (var animator in GetComponentsInChildren<Animator>())
            {
                if (animator.enabled && animator.runtimeAnimatorController != null)
                {
                    Animator = animator;
                    break;
                }
            }

            if (Animator != null)
            {
                Animator.runtimeAnimatorController = CharacterAnimator.CreateController(Animator.runtimeAnimatorController, definition);
                Animator.SetFloat(CharacterAnimator.MoveSpeedParameter, definition.moveAnimationSpeed);
            }
        }

        public void Move(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction = Vector3.ClampMagnitude(direction, 1f);
            var start = transform.position;
            var target = start + direction * definition.moveSpeed * deltaTime;
            if (moveBounds.width > 0f && moveBounds.height > 0f)
            {
                target.x = Mathf.Clamp(target.x, moveBounds.xMin, moveBounds.xMax);
                target.z = Mathf.Clamp(target.z, moveBounds.yMin, moveBounds.yMax);
            }

            var delta = target - start;
            if (IsWater(start + delta))
            {
                if (!IsWater(start + new Vector3(delta.x, 0f, 0f)))
                    delta = new Vector3(delta.x, 0f, 0f);
                else if (!IsWater(start + new Vector3(0f, 0f, delta.z)))
                    delta = new Vector3(0f, 0f, delta.z);
                else
                    delta = Vector3.zero;
            }

            if (controller != null && controller.enabled)
                controller.Move(delta);
            else
                transform.position = start + delta;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 15f * deltaTime);
            var moved = transform.position - start;
            moved.y = 0f;
            speedThisFrame = deltaTime > 0f ? moved.magnitude / deltaTime : 0f;
        }

        bool IsWater(Vector3 point)
        {
            var hits = Physics.RaycastAll(point + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool water = false;
            foreach (var hit in hits)
            {
                if (hit.collider == controller || hit.distance >= nearest)
                    continue;
                nearest = hit.distance;
                water = hit.collider.gameObject.layer == WaterLayer;
            }
            return water;
        }

        static int WaterLayer => LayerMask.NameToLayer("Water");

        void Update()
        {
            if (controller == null || !controller.enabled)
                return;

            verticalSpeed = controller.isGrounded ? -2f : verticalSpeed + Gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalSpeed * Time.deltaTime);
        }

        public bool UseAbility()
        {
            if (!Ability.TryUse(definition))
                return false;

            if (Animator != null && definition.attackClip != null)
                Animator.SetTrigger(CharacterAnimator.AttackTrigger);
            else
                StartCoroutine(AttackPunch());

            if (IsometricCamera.Active != null)
                IsometricCamera.Active.Shake(definition.cameraShakeStrength, definition.cameraShakeDuration);

            var effect = AttackEffect.Spawn(definition, transform);
            if (effect != null)
            {
                foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>())
                    particles.Play(false);
                Destroy(effect, AttackEffect.Duration(effect));
            }
            return true;
        }

        void LateUpdate()
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
                Animator.SetFloat(CharacterAnimator.SpeedParameter, speedThisFrame);
            speedThisFrame = 0f;
        }

        IEnumerator AttackPunch()
        {
            var visual = transform.Find(VisualChildName);
            if (visual == null)
                yield break;
            var start = visual.localScale;
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                visual.localScale = start * (1f + 0.2f * Mathf.Sin(t / 0.2f * Mathf.PI));
                yield return null;
            }
            visual.localScale = start;
        }
    }
}
