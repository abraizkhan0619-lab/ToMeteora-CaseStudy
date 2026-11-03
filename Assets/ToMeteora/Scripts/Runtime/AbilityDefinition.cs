using System;
using UnityEngine;

namespace ToMeteora
{
    public abstract class AbilityDefinition : ScriptableObject
    {
        public string displayName = "Ability";
        public float damage = 10f;
        public float range = 2f;

        public abstract Type BehaviourType { get; }
    }
}
