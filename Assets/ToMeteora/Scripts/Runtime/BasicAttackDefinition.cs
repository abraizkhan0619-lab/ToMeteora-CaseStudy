using System;
using UnityEngine;

namespace ToMeteora
{
    [CreateAssetMenu(menuName = "To Meteora/Abilities/Basic Attack", fileName = "BasicAttack")]
    public class BasicAttackDefinition : AbilityDefinition
    {
        public override Type BehaviourType => typeof(BasicAttack);
    }
}
