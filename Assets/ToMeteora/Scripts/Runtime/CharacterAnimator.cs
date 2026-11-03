using UnityEngine;

namespace ToMeteora
{
    public static class CharacterAnimator
    {
        public const string SpeedParameter = "Speed";
        public const string MoveSpeedParameter = "MoveAnimSpeed";
        public const string AttackTrigger = "Attack";

        public const string IdleSlot = "Idle";
        public const string MoveSlot = "Move";
        public const string AttackSlot = "Attack";

        public static RuntimeAnimatorController CreateController(RuntimeAnimatorController baseController, CharacterDefinition definition)
        {
            if (definition.customController != null)
                return definition.customController;

            var controller = new AnimatorOverrideController(baseController) { name = definition.id };
            if (definition.idleClip != null)
                controller[IdleSlot] = definition.idleClip;
            if (definition.moveClip != null)
                controller[MoveSlot] = definition.moveClip;
            if (definition.attackClip != null)
                controller[AttackSlot] = definition.attackClip;
            return controller;
        }
    }
}
