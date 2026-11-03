using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToMeteora.Tools
{
    public static class CharacterBuilder
    {
        public const string DefaultOutputFolder = "Assets/ToMeteora/Prefabs/Characters";
        public const string DefaultRosterPath = "Assets/ToMeteora/Resources/CharacterRoster.asset";
        public const string BaseControllerPath = "Assets/ToMeteora/Animations/CharacterBase.controller";
        public const string VisualChildName = HeroCharacter.VisualChildName;

        public static List<string> CreateOrUpdate(CharacterDefinition def) =>
            CreateOrUpdate(def, LoadOrCreateRoster(DefaultRosterPath), DefaultOutputFolder);

        public static List<string> CreateOrUpdate(CharacterDefinition def, CharacterRoster roster, string outputFolder)
        {
            var errors = CharacterValidator.Validate(def);
            if (errors.Count > 0)
                return errors;

            EnsureFolder(outputFolder);
            string prefabPath = PrefabPathFor(def, outputFolder);

            string renameError = MoveIfIdChanged(def, roster, prefabPath);
            if (renameError != null)
                return new List<string> { renameError };

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null;
            try
            {
                BuildPrefab(def, prefabPath, isNew);
                Register(roster, def, AssetDatabase.LoadAssetAtPath<HeroCharacter>(prefabPath));
            }
            catch (Exception e)
            {
                if (isNew)
                    AssetDatabase.DeleteAsset(prefabPath);
                return new List<string> { $"Setup failed and was undone: {e.Message}" };
            }

            return errors;
        }

        public static List<string> CreatePlaceholder(CharacterDefinition def) =>
            CreatePlaceholder(def, LoadOrCreateRoster(DefaultRosterPath), DefaultOutputFolder);

        public static List<string> CreatePlaceholder(CharacterDefinition def, CharacterRoster roster, string outputFolder)
        {
            var errors = CharacterValidator.ValidateId(def);
            if (errors.Count > 0)
                return errors;

            EnsureFolder(outputFolder);
            string prefabPath = PrefabPathFor(def, outputFolder);

            string renameError = MoveIfIdChanged(def, roster, prefabPath);
            if (renameError != null)
                return new List<string> { renameError };

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var root = new GameObject(def.id);
                try
                {
                    root.AddComponent<HeroCharacter>().definition = def;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            Register(roster, def, AssetDatabase.LoadAssetAtPath<HeroCharacter>(prefabPath));
            return errors;
        }

        static string MoveIfIdChanged(CharacterDefinition def, CharacterRoster roster, string prefabPath)
        {
            var entry = roster.FindByDefinition(def);
            if (entry == null || entry.prefab == null)
                return null;

            string oldPath = AssetDatabase.GetAssetPath(entry.prefab);
            if (oldPath == prefabPath)
                return null;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return $"Cannot rename '{oldPath}': '{prefabPath}' already exists. Delete it or choose another ID.";

            string moveError = AssetDatabase.MoveAsset(oldPath, prefabPath);
            return string.IsNullOrEmpty(moveError) ? null : $"Cannot rename '{oldPath}' to '{prefabPath}': {moveError}";
        }

        public static List<string> CreateOrUpdateAll()
        {
            var errors = new List<string>();
            foreach (var def in CharacterValidator.FindAllDefinitions())
                foreach (var error in CreateOrUpdate(def))
                    errors.Add($"{def.name}: {error}");
            return errors;
        }

        public static void CreateOrUpdateAllFromCommandLine()
        {
            var errors = CreateOrUpdateAll();
            foreach (var error in errors)
                Debug.LogError(error);
            Debug.Log($"Character setup finished with {errors.Count} error(s).");
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        public static string PrefabPathFor(CharacterDefinition def, string outputFolder) =>
            $"{outputFolder}/{def.id}.prefab";

        public static RuntimeAnimatorController LoadBaseController() =>
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);

        static void BuildPrefab(CharacterDefinition def, string path, bool isNew)
        {
            bool fresh = isNew || HasMissingNestedPrefab(path);
            if (fresh && !isNew)
                Debug.LogWarning($"'{path}' contains a prefab that no longer exists (was a visual deleted?). It is rebuilt from scratch; manual additions to it are lost.");

            GameObject root = fresh ? new GameObject(def.id) : PrefabUtility.LoadPrefabContents(path);
            try
            {
                var hero = root.GetComponent<HeroCharacter>();
                if (hero == null)
                    hero = root.AddComponent<HeroCharacter>();
                hero.definition = def;

                foreach (var behaviour in root.GetComponents<AbilityBehaviour>())
                {
                    if (behaviour.GetType() != def.ability.BehaviourType)
                        Object.DestroyImmediate(behaviour);
                }
                if (root.GetComponent(def.ability.BehaviourType) == null)
                    root.AddComponent(def.ability.BehaviourType);

                var visual = root.transform.Find(VisualChildName);
                bool visualIsCurrent = visual != null &&
                    PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject) == def.visualPrefab;
                if (!visualIsCurrent)
                {
                    if (visual != null)
                        Object.DestroyImmediate(visual.gameObject);
                    visual = ((GameObject)PrefabUtility.InstantiatePrefab(def.visualPrefab, root.transform)).transform;
                    visual.name = VisualChildName;
                }
                FitVisual(visual, def);
                SetupAnimator(visual, def);
                SetupController(root, visual, def);

                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                if (!saved)
                    throw new Exception($"Unity could not save '{path}'.");
            }
            finally
            {
                if (fresh)
                    Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static readonly Regex SourcePrefabGuid = new Regex(@"m_SourcePrefab: \{fileID: -?\d+, guid: (\w+)");

        static bool HasMissingNestedPrefab(string path)
        {
            foreach (Match match in SourcePrefabGuid.Matches(File.ReadAllText(path)))
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                if (string.IsNullOrEmpty(sourcePath) || AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                    return true;
            }
            return false;
        }

        public static void FitVisual(Transform visual, CharacterDefinition def)
        {
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.Euler(0f, def.visualYaw, 0f);
            visual.localScale = def.visualPrefab.transform.localScale;

            var bounds = GetBounds(visual);
            visual.localScale *= def.visualHeight / bounds.size.y;

            bounds = GetBounds(visual);
            var root = visual.parent != null ? visual.parent.position : Vector3.zero;
            visual.localPosition = new Vector3(root.x - bounds.center.x, root.y - bounds.min.y, root.z - bounds.center.z);
        }

        static void SetupAnimator(Transform visual, CharacterDefinition def)
        {
            var target = def.HasAnimation ? FindAnimationRoot(visual, def) : null;

            foreach (var other in visual.GetComponentsInChildren<Animator>(true))
            {
                if (other.transform == target)
                    continue;
                if (PrefabUtility.IsAddedComponentOverride(other))
                    Object.DestroyImmediate(other);
                else
                {
                    other.runtimeAnimatorController = null;
                    other.enabled = false;
                }
            }

            if (target == null)
                return;

            var animator = target.GetComponent<Animator>();
            if (animator == null)
                animator = target.gameObject.AddComponent<Animator>();
            animator.enabled = true;
            animator.runtimeAnimatorController = def.customController != null ? def.customController : LoadBaseController();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        public static Transform FindAnimationRoot(Transform visual, CharacterDefinition def) =>
            FindAnimationRoot(visual, SampleClip(def));

        public static Transform FindAnimationRoot(Transform visual, AnimationClip clip)
        {
            if (clip == null)
                return visual;

            var paths = AnimationUtility.GetCurveBindings(clip)
                .Select(b => b.path)
                .Where(p => p.Length > 0)
                .Distinct()
                .Take(30)
                .ToArray();
            if (paths.Length == 0)
                return visual;

            foreach (var candidate in visual.GetComponentsInChildren<Transform>(true))
            {
                if (paths.All(p => candidate.Find(p) != null))
                    return candidate;
            }
            return null;
        }

        static AnimationClip SampleClip(CharacterDefinition def)
        {
            if (def.customController != null)
                return def.customController.animationClips.FirstOrDefault(c => c != null);
            return def.idleClip != null ? def.idleClip : def.moveClip != null ? def.moveClip : def.attackClip;
        }

        public static void MakeClipsLoop(CharacterDefinition def)
        {
            foreach (var clip in new[] { def.idleClip, def.moveClip })
            {
                if (clip == null || CharacterValidator.Loops(clip))
                    continue;
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
            }
            AssetDatabase.SaveAssets();
        }

        public static Bounds GetBounds(Transform visual)
        {
            Bounds? bounds = null;
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                var b = r is SkinnedMeshRenderer skinned ? MeshBounds(skinned) : r.bounds;
                if (bounds == null)
                    bounds = b;
                else
                {
                    var merged = bounds.Value;
                    merged.Encapsulate(b);
                    bounds = merged;
                }
            }
            return bounds ?? new Bounds(visual.position, Vector3.zero);
        }

        static Bounds MeshBounds(SkinnedMeshRenderer skinned)
        {
            var mesh = new Mesh();
            skinned.BakeMesh(mesh, true);
            var toWorld = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
            var vertices = mesh.vertices;
            var bounds = new Bounds(toWorld.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            foreach (var v in vertices)
                bounds.Encapsulate(toWorld.MultiplyPoint3x4(v));
            Object.DestroyImmediate(mesh);
            return bounds;
        }

        static void SetupController(GameObject root, Transform visual, CharacterDefinition def)
        {
            var controller = root.GetComponent<CharacterController>();
            if (controller == null)
                controller = root.AddComponent<CharacterController>();

            var size = GetBounds(visual).size;
            float footprint = Mathf.Min(size.x, size.z);
            controller.height = def.visualHeight;
            controller.radius = Mathf.Clamp(footprint * 0.4f, 0.2f, def.visualHeight * 0.5f);
            controller.skinWidth = 0.05f;
            controller.center = new Vector3(0f, def.visualHeight * 0.5f + controller.skinWidth, 0f);
            controller.stepOffset = Mathf.Min(0.4f, def.visualHeight * 0.3f);
            controller.slopeLimit = 50f;
        }

        static void Register(CharacterRoster roster, CharacterDefinition def, HeroCharacter prefab)
        {
            roster.entries.RemoveAll(e => e.definition == null);

            var entry = roster.FindByDefinition(def);
            if (entry == null)
            {
                entry = new CharacterRoster.Entry { definition = def };
                roster.entries.Add(entry);
            }
            entry.prefab = prefab;

            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssetIfDirty(roster);
        }

        public static CharacterRoster LoadOrCreateRoster(string path)
        {
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(path);
            if (roster != null)
                return roster;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            roster = ScriptableObject.CreateInstance<CharacterRoster>();
            AssetDatabase.CreateAsset(roster, path);
            return roster;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
