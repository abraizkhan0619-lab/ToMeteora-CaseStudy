using System.Collections;
using UnityEngine;

namespace ToMeteora
{
    [SelectionBase]
    public class Enemy : MonoBehaviour
    {
        public float maxHealth = 60f;
        [Tooltip("Seconds before a defeated enemy comes back.")]
        public float respawnSeconds = 3f;
        public Color hitFlashColor = Color.white;
        [Tooltip("Health bar shown above the enemy.")]
        public EnemyHealthBar healthBar;

        public float CurrentHealth { get; private set; }
        public int TimesHit { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        Renderer[] renderers;
        Collider[] colliders;
        Color baseColor;
        Vector3 basePosition;
        Quaternion baseRotation;
        Vector3 baseScale;
        MaterialPropertyBlock block;

        public void CentreOnBody()
        {
            var body = GetComponentInChildren<MeshRenderer>();
            if (body == null || body.transform == transform)
                return;

            var offset = body.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.000001f)
                return;

            transform.position += offset;
            body.transform.position -= offset;
        }

        void Awake()
        {
            CentreOnBody();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            var body = renderers.Length > 0 ? renderers[0] : null;
            baseColor = body != null && body.sharedMaterial.HasProperty("_BaseColor") ? body.sharedMaterial.GetColor("_BaseColor") : Color.red;
            basePosition = transform.position;
            baseRotation = transform.rotation;
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            CurrentHealth = maxHealth;
            if (healthBar != null)
                healthBar.Set(1f, true);
        }

        public void TakeHit(float damage)
        {
            if (!IsAlive)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            TimesHit++;
            if (healthBar != null)
                healthBar.Set(CurrentHealth / maxHealth);

            StopAllCoroutines();
            transform.localScale = baseScale;
            StartCoroutine(IsAlive ? Flash() : Defeat());
        }

        IEnumerator Flash()
        {
            SetColor(hitFlashColor);
            yield return new WaitForSeconds(0.1f);
            SetColor(DamagedColor());
        }

        IEnumerator Defeat()
        {
            SetColor(hitFlashColor);
            yield return Scale(1f, 0f, 0.3f);
            SetVisible(false);

            yield return new WaitForSeconds(respawnSeconds);

            transform.SetPositionAndRotation(basePosition, baseRotation);
            CurrentHealth = maxHealth;
            SetColor(baseColor);
            SetVisible(true);
            if (healthBar != null)
                healthBar.Set(1f, true);
            yield return Scale(0f, 1f, 0.35f);
        }

        IEnumerator Scale(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(from, to, t / seconds);
                transform.localScale = baseScale * k;
                yield return null;
            }
            transform.localScale = baseScale * to;
        }

        Color DamagedColor() => Color.Lerp(baseColor * 0.35f, baseColor, CurrentHealth / maxHealth);

        void SetColor(Color color)
        {
            block.SetColor("_BaseColor", color);
            foreach (var r in renderers)
            {
                if (r is MeshRenderer)
                    r.SetPropertyBlock(block);
            }
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers)
                r.enabled = visible;
            foreach (var c in colliders)
                c.enabled = visible;
            if (healthBar != null)
                healthBar.gameObject.SetActive(visible);
        }
    }
}
