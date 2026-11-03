using System.Collections;
using UnityEngine;

namespace ToMeteora
{
    public class Enemy : MonoBehaviour
    {
        public float maxHealth = 60f;
        [Tooltip("Seconds before a defeated enemy comes back.")]
        public float respawnSeconds = 3f;
        public Color hitFlashColor = Color.white;

        public float CurrentHealth { get; private set; }
        public int TimesHit { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        Renderer[] renderers;
        Collider[] colliders;
        Color baseColor;
        Vector3 baseScale;
        MaterialPropertyBlock block;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            baseColor = renderers.Length > 0 ? renderers[0].sharedMaterial.GetColor("_BaseColor") : Color.red;
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            CurrentHealth = maxHealth;
        }

        public void TakeHit(float damage)
        {
            if (!IsAlive)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            TimesHit++;
            StopAllCoroutines();
            StartCoroutine(IsAlive ? Flash() : Defeat());
        }

        IEnumerator Flash()
        {
            SetColor(hitFlashColor);
            transform.localScale = baseScale * 1.15f;
            yield return new WaitForSeconds(0.12f);
            transform.localScale = baseScale;
            SetColor(DamagedColor());
        }

        IEnumerator Defeat()
        {
            SetColor(hitFlashColor);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                transform.localScale = baseScale * (1f - t / 0.3f);
                yield return null;
            }
            SetVisible(false);

            yield return new WaitForSeconds(respawnSeconds);

            CurrentHealth = maxHealth;
            transform.localScale = baseScale;
            SetColor(baseColor);
            SetVisible(true);
        }

        Color DamagedColor() => Color.Lerp(baseColor * 0.35f, baseColor, CurrentHealth / maxHealth);

        void SetColor(Color color)
        {
            block.SetColor("_BaseColor", color);
            foreach (var r in renderers)
                r.SetPropertyBlock(block);
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers)
                r.enabled = visible;
            foreach (var c in colliders)
                c.enabled = visible;
        }
    }
}
