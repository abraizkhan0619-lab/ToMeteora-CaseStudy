using UnityEngine;

namespace ToMeteora
{
    public static class AttackEffect
    {
        public static GameObject Spawn(CharacterDefinition definition, Transform character)
        {
            if (definition.attackEffect == null)
                return null;

            var parent = definition.attackEffectFollowsCharacter ? character : null;
            var effect = Object.Instantiate(definition.attackEffect,
                character.TransformPoint(definition.attackEffectOffset), character.rotation, parent);
            effect.transform.localScale = definition.attackEffect.transform.localScale * definition.attackEffectScale;

            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            return effect;
        }

        public static float Duration(GameObject effect)
        {
            float duration = 0.5f;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax);
            }
            return duration;
        }
    }
}
