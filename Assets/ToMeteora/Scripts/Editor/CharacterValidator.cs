using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ToMeteora.Tools
{
    public static class CharacterValidator
    {
        static readonly Regex IdPattern = new Regex("^[a-z][a-z0-9_]*$");

        public static List<string> Validate(CharacterDefinition def)
        {
            var errors = new List<string>();
            if (def == null)
            {
                errors.Add("No character definition selected.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(def.id))
                errors.Add("Stable ID is empty. Set an ID such as 'hero_angelo'.");
            else if (!IdPattern.IsMatch(def.id))
                errors.Add($"Stable ID '{def.id}' is not allowed. Use lowercase letters, numbers and '_' only, starting with a letter (e.g. 'hero_angelo').");

            if (string.IsNullOrWhiteSpace(def.displayName))
                errors.Add("Display Name is empty. Enter the name players will see.");

            if (def.visualPrefab == null)
                errors.Add("Visual Prefab is missing. Drag the character's model prefab into the 'Visual Prefab' field.");
            else if (def.visualPrefab.GetComponentInChildren<Renderer>() == null)
                errors.Add($"Visual Prefab '{def.visualPrefab.name}' has nothing to show (no mesh renderer). Pick a character model prefab.");

            if (def.visualHeight <= 0f)
                errors.Add($"Visual Height is {def.visualHeight}. It must be greater than 0.");

            ValidateAnimation(def, errors);

            if (def.attackEffect != null)
            {
                if (def.attackEffect.GetComponentInChildren<ParticleSystem>(true) == null)
                    errors.Add($"Attack Effect '{def.attackEffect.name}' has no Particle System. Pick a particle prefab, or leave it empty.");
                if (def.attackEffectScale <= 0f)
                    errors.Add($"Attack Effect Scale is {def.attackEffectScale}. It must be greater than 0.");
            }

            if (def.health <= 0f)
                errors.Add($"Health is {def.health}. It must be greater than 0.");
            if (def.moveSpeed <= 0f)
                errors.Add($"Move Speed is {def.moveSpeed}. It must be greater than 0.");
            if (def.cooldown <= 0f)
                errors.Add($"Cooldown is {def.cooldown}. It must be greater than 0 seconds.");

            if (def.ability == null)
                errors.Add("Ability is missing. Pick an ability asset, e.g. 'BasicAttack'.");

            if (!string.IsNullOrWhiteSpace(def.id))
            {
                foreach (var other in FindAllDefinitions())
                {
                    if (other != def && other.id == def.id)
                        errors.Add($"ID '{def.id}' is also used by '{AssetDatabase.GetAssetPath(other)}'. Each character needs its own ID.");
                }
            }

            return errors;
        }

        static void ValidateAnimation(CharacterDefinition def, List<string> errors)
        {
            if (!def.HasAnimation)
                return;

            if (def.customController != null)
            {
                var parameters = (def.customController as UnityEditor.Animations.AnimatorController)?.parameters;
                if (parameters != null)
                {
                    if (!parameters.Any(p => p.name == CharacterAnimator.SpeedParameter && p.type == AnimatorControllerParameterType.Float))
                        errors.Add($"Custom Controller '{def.customController.name}' needs a float parameter '{CharacterAnimator.SpeedParameter}'.");
                    if (!parameters.Any(p => p.name == CharacterAnimator.AttackTrigger && p.type == AnimatorControllerParameterType.Trigger))
                        errors.Add($"Custom Controller '{def.customController.name}' needs a trigger parameter '{CharacterAnimator.AttackTrigger}'.");
                }
                return;
            }

            if (def.idleClip == null)
                errors.Add("Idle Clip is missing. Animated characters need both an Idle and a Move clip.");
            else if (!Loops(def.idleClip))
                errors.Add($"Idle Clip '{def.idleClip.name}' does not loop. Click 'Make Clips Loop' or tick Loop Time on the clip.");

            if (def.moveClip == null)
                errors.Add("Move Clip is missing. Animated characters need both an Idle and a Move clip.");
            else if (!Loops(def.moveClip))
                errors.Add($"Move Clip '{def.moveClip.name}' does not loop. Click 'Make Clips Loop' or tick Loop Time on the clip.");

            if (def.moveAnimationSpeed <= 0f)
                errors.Add($"Move Animation Speed is {def.moveAnimationSpeed}. It must be greater than 0.");

            if (CharacterBuilder.LoadBaseController() == null)
                errors.Add($"The shared animator controller is missing at '{CharacterBuilder.BaseControllerPath}'.");

            ValidateClipsFitModel(def, errors);
        }

        static void ValidateClipsFitModel(CharacterDefinition def, List<string> errors)
        {
            if (def.visualPrefab == null)
                return;

            foreach (var clip in new[] { def.idleClip, def.moveClip, def.attackClip })
            {
                if (clip != null && CharacterBuilder.FindAnimationRoot(def.visualPrefab.transform, clip) == null)
                    errors.Add($"Clip '{clip.name}' does not fit '{def.visualPrefab.name}': its bones were not found in the model. Use clips made for this model.");
            }
        }

        public static bool Loops(AnimationClip clip) => AnimationUtility.GetAnimationClipSettings(clip).loopTime;

        public static List<CharacterDefinition> FindAllDefinitions()
        {
            var result = new List<CharacterDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(CharacterDefinition)))
            {
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null)
                    result.Add(def);
            }
            return result;
        }
    }
}
