using UnityEngine;

namespace ToMeteora
{
    public abstract class AbilityBehaviour : MonoBehaviour
    {
        float readyAtTime;

        public float CooldownRemaining => Mathf.Max(0f, readyAtTime - Time.time);
        public int TimesUsed { get; private set; }

        public bool TryUse(CharacterDefinition character)
        {
            if (CooldownRemaining > 0f)
                return false;

            readyAtTime = Time.time + character.cooldown;
            TimesUsed++;
            Execute(character.ability);
            return true;
        }

        protected abstract void Execute(AbilityDefinition ability);
    }
}
