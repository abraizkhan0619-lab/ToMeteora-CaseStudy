using UnityEngine;

namespace ToMeteora
{
    public class BasicAttack : AbilityBehaviour
    {
        protected override void Execute(AbilityDefinition ability)
        {
            Debug.Log($"{name} used {ability.displayName} (damage {ability.damage}, range {ability.range})");
        }
    }
}
