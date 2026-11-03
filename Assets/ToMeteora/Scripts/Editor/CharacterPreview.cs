using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToMeteora.Tools
{
    public class CharacterPreview : IDisposable
    {
        public Vector2 orbit = new Vector2(150f, 12f);

        readonly PreviewRenderUtility utility = new PreviewRenderUtility();
        GameObject root;
        Transform visual;
        SkinnedMeshRenderer[] skinned = Array.Empty<SkinnedMeshRenderer>();
        Mesh[] baked = Array.Empty<Mesh>();
        (GameObject prefab, float height, float yaw) shown;
        AnimationClip targetClip;
        GameObject animationTarget;
        Vector3 frameCenter = Vector3.up;
        float frameRadius = 1f;

        public void Show(CharacterDefinition def)
        {
            var key = (def.visualPrefab, def.visualHeight, def.visualYaw);
            if (root != null && key.Equals(shown))
                return;

            Clear();
            root = new GameObject("CharacterPreview");
            var instance = Object.Instantiate(def.visualPrefab, root.transform);
            CharacterBuilder.FitVisual(instance.transform, def);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            baked = skinned.Select(_ => new Mesh { hideFlags = HideFlags.HideAndDontSave }).ToArray();
            foreach (var renderer in skinned)
                renderer.forceRenderingOff = true;

            visual = instance.transform;
            targetClip = null;
            animationTarget = null;
            shown = key;
            utility.AddSingleGO(root);
            FrameCharacter(def);
        }

        void FrameCharacter(CharacterDefinition def)
        {
            Bounds? bounds = null;
            for (int i = 0; i < skinned.Length; i++)
            {
                skinned[i].BakeMesh(baked[i], true);
                var matrix = BakedMatrix(skinned[i]);
                foreach (var v in baked[i].vertices)
                {
                    var p = matrix.MultiplyPoint3x4(v);
                    if (bounds == null)
                        bounds = new Bounds(p, Vector3.zero);
                    else
                    {
                        var b = bounds.Value;
                        b.Encapsulate(p);
                        bounds = b;
                    }
                }
            }
            if (bounds == null)
                bounds = CharacterBuilder.GetBounds(visual);

            frameCenter = bounds.Value.center;
            frameRadius = Mathf.Max(bounds.Value.extents.magnitude, def.visualHeight * 0.5f);
        }

        static Matrix4x4 BakedMatrix(SkinnedMeshRenderer renderer) =>
            Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);

        public void Sample(AnimationClip clip, float time)
        {
            if (clip == null || visual == null)
                return;

            if (clip != targetClip)
            {
                var target = CharacterBuilder.FindAnimationRoot(visual, clip);
                animationTarget = target != null ? target.gameObject : null;
                targetClip = clip;
            }
            if (animationTarget != null)
                clip.SampleAnimation(animationTarget, clip.length > 0f ? time % clip.length : 0f);
        }

        public void Draw(Rect rect)
        {
            utility.BeginPreview(rect, GUIStyle.none);
            DrawScene();
            utility.EndAndDrawPreview(rect);
        }

        public Texture2D DrawToTexture(Rect rect)
        {
            utility.BeginStaticPreview(rect);
            DrawScene();
            return utility.EndStaticPreview();
        }

        void DrawScene()
        {
            var cam = utility.camera;
            cam.fieldOfView = 30f;
            float distance = frameRadius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = distance + frameRadius * 4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.17f, 0.19f, 0.23f);
            cam.transform.position = frameCenter + Quaternion.Euler(orbit.y, orbit.x, 0f) * (Vector3.back * distance);
            cam.transform.LookAt(frameCenter);

            utility.lights[0].intensity = 1.3f;
            utility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            utility.lights[1].intensity = 0.6f;
            utility.ambientColor = new Color(0.35f, 0.37f, 0.42f);

            for (int i = 0; i < skinned.Length; i++)
            {
                var renderer = skinned[i];
                renderer.BakeMesh(baked[i], true);
                var matrix = BakedMatrix(renderer);
                var materials = renderer.sharedMaterials;
                for (int sub = 0; sub < baked[i].subMeshCount && materials.Length > 0; sub++)
                    utility.DrawMesh(baked[i], matrix, materials[Mathf.Min(sub, materials.Length - 1)], sub);
            }

            utility.Render(true);
        }

        public void Clear()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            foreach (var mesh in baked)
                Object.DestroyImmediate(mesh);
            root = null;
            visual = null;
            skinned = Array.Empty<SkinnedMeshRenderer>();
            baked = Array.Empty<Mesh>();
        }

        public void Dispose()
        {
            Clear();
            utility.Cleanup();
        }
    }
}
