using System.Collections.Generic;
using UnityEngine;

namespace ToMeteora
{
    public class BasicAttack : AbilityBehaviour
    {
        readonly HashSet<Enemy> hits = new HashSet<Enemy>();

        public Vector3 AreaCentre(AbilityDefinition ability) =>
            transform.position + transform.forward * ability.range;

        protected override void Execute(AbilityDefinition ability)
        {
            var centre = AreaCentre(ability);
            hits.Clear();
            foreach (var collider in Physics.OverlapSphere(centre, ability.radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var enemy = collider.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.IsAlive && hits.Add(enemy))
                    enemy.TakeHit(ability.damage);
            }
            LastHitCount = hits.Count;

            ShowArea(ability, centre);
        }

        static void ShowArea(AbilityDefinition ability, Vector3 centre)
        {
            if (ability.areaIndicator == null)
                return;

            var indicator = Instantiate(ability.areaIndicator, centre + Vector3.up * 0.05f, Quaternion.identity);
            float duration = 0.5f;
            foreach (var particles in indicator.GetComponentsInChildren<ParticleSystem>())
            {
                var shape = particles.shape;
                shape.radius = ability.radius;
                var main = particles.main;
                duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax);
                particles.Play(false);
            }
            Destroy(indicator, duration);
        }
    }
}
