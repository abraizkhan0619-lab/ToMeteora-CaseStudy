using System;
using UnityEngine;

namespace ToMeteora
{
    public abstract class AbilityDefinition : ScriptableObject
    {
        public string displayName = "Ability";
        [Tooltip("Damage dealt to each enemy inside the area.")]
        public float damage = 20f;
        [Tooltip("How far in front of the character the area is centred. 0 = around the character.")]
        public float range = 1.5f;
        [Tooltip("Enemies within this radius of the area centre are hit.")]
        public float radius = 3f;
        [Tooltip("Optional particle prefab that shows the area when the ability is used. Its circle is sized to the radius.")]
        public GameObject areaIndicator;

        public abstract Type BehaviourType { get; }
    }
}
