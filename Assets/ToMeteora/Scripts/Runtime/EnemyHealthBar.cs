using UnityEngine;
using UnityEngine.UI;

namespace ToMeteora
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [Tooltip("Filled image that shows current health.")]
        public Image fill;
        [Tooltip("Optional filled image behind the fill that catches up slowly, showing the damage just taken.")]
        public Image damageTrail;
        public Gradient healthColors;
        [Tooltip("How fast the fill moves to the new value, per second.")]
        public float fillSpeed = 3f;
        [Tooltip("How fast the damage trail catches up, per second.")]
        public float trailSpeed = 0.8f;
        [Tooltip("Seconds the trail waits before it starts to catch up.")]
        public float trailDelay = 0.35f;

        float target = 1f;
        float shown = 1f;
        float trail = 1f;
        float trailWait;

        public void Set(float normalized, bool instant = false)
        {
            target = Mathf.Clamp01(normalized);
            trailWait = trailDelay;
            if (instant)
                shown = trail = target;
            Draw();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = cam.transform.rotation;

            shown = Mathf.MoveTowards(shown, target, fillSpeed * Time.deltaTime);
            if (trailWait > 0f)
                trailWait -= Time.deltaTime;
            else
                trail = Mathf.MoveTowards(trail, shown, trailSpeed * Time.deltaTime);
            Draw();
        }

        void Draw()
        {
            if (fill != null)
            {
                fill.fillAmount = shown;
                if (healthColors != null)
                    fill.color = healthColors.Evaluate(shown);
            }
            if (damageTrail != null)
                damageTrail.fillAmount = Mathf.Max(trail, shown);
        }
    }
}
