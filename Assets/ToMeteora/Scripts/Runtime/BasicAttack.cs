using UnityEngine;

namespace ToMeteora
{
    public class BasicAttack : AbilityBehaviour
    {
        protected override void Execute(AbilityDefinition ability)
        {
            var effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(effect.GetComponent<Collider>());
            effect.name = "AttackEffect";
            effect.transform.position = transform.position + Vector3.up + transform.forward * ability.range * 0.5f;
            effect.transform.localScale = Vector3.one * ability.range * 0.5f;
            Destroy(effect, 0.15f);

            Debug.Log($"{name} used {ability.displayName} (damage {ability.damage}, range {ability.range})");
        }
    }
}
