using UnityEngine;

namespace ToMeteora
{
    [CreateAssetMenu(menuName = "To Meteora/Character Definition", fileName = "NewCharacter")]
    public class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Permanent ID used by saves and the roster. Lowercase, e.g. hero_angelo. Do not change after release.")]
        public string id;
        public string displayName;

        [Header("Visual")]
        [Tooltip("The character model prefab.")]
        public GameObject visualPrefab;
        [Tooltip("The model is scaled to this height in Unity units, so models of any size work.")]
        public float visualHeight = 2f;
        [Tooltip("Turns the model (degrees) so it faces forward (+Z).")]
        public float visualYaw = 0f;

        [Header("Animation")]
        [Tooltip("Looping idle clip.")]
        public AnimationClip idleClip;
        [Tooltip("Looping walk or run clip, ideally in place.")]
        public AnimationClip moveClip;
        [Tooltip("Optional. Played when the ability is used.")]
        public AnimationClip attackClip;
        [Tooltip("Playback speed of the move clip. Raise it if the feet slide.")]
        public float moveAnimationSpeed = 1f;
        [Tooltip("Optional. Replaces the shared controller. Must have a float 'Speed' and a trigger 'Attack'.")]
        public RuntimeAnimatorController customController;

        [Header("Stats")]
        public float health = 100f;
        [Tooltip("Unity units per second.")]
        public float moveSpeed = 5f;
        [Tooltip("Seconds between ability uses.")]
        public float cooldown = 3f;

        [Header("Ability")]
        public AbilityDefinition ability;

        public bool HasAnimation => customController != null || idleClip != null || moveClip != null || attackClip != null;
    }
}
