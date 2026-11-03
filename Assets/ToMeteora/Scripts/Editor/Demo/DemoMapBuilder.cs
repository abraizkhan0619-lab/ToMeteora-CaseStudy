using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ToMeteora.Tools
{
    public static class DemoMapBuilder
    {
        const string ScenePath = "Assets/ToMeteora/Scenes/CharacterTest.unity";
        const string LocationPrefabPath = "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Location with environment/camping forest with environment.prefab";
        const string SkyboxPath = "Assets/ToMeteora/Materials/DemoSky.mat";
        const string FogMaterialPath = "Assets/ToMeteora/Materials/FogOfWar.mat";
        const string PostProcessPath = "Assets/ToMeteora/Settings/DemoPostProcess.asset";
        const string HudPrefabPath = "Assets/ToMeteora/Prefabs/UI/GameHUD.prefab";
        const float EdgeMargin = 2f;
        const string EnemyPrefabPath = "Assets/ToMeteora/Prefabs/Enemies/DummyEnemy.prefab";
        const int EnemyCount = 8;

        public static void Build()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PlaceLocation(out var ground, out var everything);
            Debug.Log($"MAP: ground {ground.center} size {ground.size}; everything size {everything.size}");

            SetupLighting();
            var demo = SetupGameplay(ground, everything, out var camera, out var fog);
            SetupUi(demo);
            PlaceEnemies(demo, everything);
            SetShadowDistance(camera.GetComponent<IsometricCamera>());
            AddAtmosphere(camera.GetComponent<IsometricCamera>(), ground);

            SavePreviews(camera, fog, demo.spawnPoint, everything);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Demo map rebuilt: " + ScenePath);
        }

        static void PlaceLocation(out Bounds ground, out Bounds everything)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LocationPrefabPath);
            if (prefab == null)
                throw new Exception($"Location prefab not found: {LocationPrefabPath}");

            var location = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            int packColliders = 0;
            foreach (var packCollider in location.GetComponentsInChildren<Collider>(true))
            {
                packCollider.enabled = false;
                packColliders++;
            }
            Debug.Log($"MAP: disabled {packColliders} box colliders that came with the location pack");
            var renderers = location.GetComponentsInChildren<MeshRenderer>();
            everything = Encapsulate(renderers);

            var groundRenderers = renderers.Where(r => MeshPath(r).Contains("/Models/Lands/")).ToArray();
            if (groundRenderers.Length == 0)
                throw new Exception("No ground ('Lands') meshes found in the location prefab.");
            GroundColliders.Clear();
            foreach (var r in groundRenderers)
                GroundColliders.Add(r.gameObject.AddComponent<MeshCollider>());
            ground = Encapsulate(groundRenderers);

            int blockers = 0, water = 0;
            foreach (var r in renderers.Except(groundRenderers))
            {
                if (MeshPath(r).Contains("/Models/Waters/"))
                {
                    r.gameObject.AddComponent<MeshCollider>();
                    r.gameObject.layer = LayerMask.NameToLayer("Water");
                    water++;
                }
                else if (IsObstacle(r))
                {
                    r.gameObject.AddComponent<MeshCollider>();
                    blockers++;
                }
            }
            Debug.Log($"MAP: {GroundColliders.Count} ground, {water} water, {blockers} obstacle colliders");

            foreach (var t in location.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static readonly HashSet<Collider> GroundColliders = new HashSet<Collider>();

        static string MeshPath(Renderer r)
        {
            var filter = r.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null ? AssetDatabase.GetAssetPath(filter.sharedMesh) : "";
        }

        static bool IsObstacle(Renderer r)
        {
            string path = MeshPath(r);
            if (path.Contains("/Plants/Grass") || path.Contains("/Mushrooms"))
                return false;
            var size = r.bounds.size;
            return size.y >= 0.6f || Mathf.Max(size.x, size.z) >= 0.8f;
        }

        static void SetupLighting()
        {
            var lighting = new GameObject("Lighting").transform;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(lighting);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.color = new Color(1f, 0.92f, 0.8f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            RenderSettings.sun = sun;

            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, SkyboxPath);
            }
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.68f, 0.92f));
            sky.SetColor("_GroundColor", new Color(0.7f, 0.78f, 0.86f));
            sky.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.56f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.3f, 0.24f);
            RenderSettings.fog = false;

            SetupPostProcessing(lighting);
        }

        static void SetupPostProcessing(Transform parent)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProcessPath);
            }
            foreach (var component in profile.components.ToArray())
            {
                profile.Remove(component.GetType());
                Object.DestroyImmediate(component, true);
            }

            AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

            var color = AddOverride<ColorAdjustments>(profile);
            color.postExposure.Override(0.1f);
            color.contrast.Override(10f);
            color.saturation.Override(10f);

            var vignette = AddOverride<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.5f);

            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Post Processing").AddComponent<Volume>();
            volume.transform.SetParent(parent);
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>();
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        static void SetShadowDistance(IsometricCamera iso)
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
                return;
            pipeline.shadowDistance = Mathf.Ceil(iso.Distance + iso.mapSize);
            pipeline.shadowCascadeCount = 1;
            EditorUtility.SetDirty(pipeline);
        }

        static CharacterSelectDemo SetupGameplay(Bounds ground, Bounds everything, out Camera camera, out FogOfWar fog)
        {
            var gameplay = new GameObject("Gameplay").transform;
            float mapSize = Mathf.Max(ground.size.x, ground.size.z);

            var demo = new GameObject("CharacterSelectDemo").AddComponent<CharacterSelectDemo>();
            demo.transform.SetParent(gameplay);
            demo.roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(CharacterBuilder.DefaultRosterPath);
            demo.playArea = Rect.MinMaxRect(
                ground.min.x + EdgeMargin, ground.min.z + EdgeMargin,
                ground.max.x - EdgeMargin, ground.max.z - EdgeMargin);
            demo.spawnPoint = FindSpawnPoint(ground, everything);
            Debug.Log($"MAP: spawn at {demo.spawnPoint}");
            AddPlayerStart(demo, gameplay);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(gameplay);
            camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var cameraData = camGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthOption = CameraOverrideOption.On;
            var iso = camGo.AddComponent<IsometricCamera>();
            iso.demo = demo;
            iso.mapCenter = new Vector3(ground.center.x, ground.max.y, ground.center.z);
            iso.mapSize = mapSize;
            iso.propHeight = everything.max.y - ground.max.y;
            iso.Fit();

            var fogMaterial = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
            if (fogMaterial == null)
            {
                fogMaterial = new Material(Shader.Find("ToMeteora/FogOfWar"));
                AssetDatabase.CreateAsset(fogMaterial, FogMaterialPath);
            }
            var fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "Fog Of War";
            fogPlane.transform.SetParent(gameplay);
            Object.DestroyImmediate(fogPlane.GetComponent<Collider>());
            fogPlane.transform.position = new Vector3(ground.center.x, everything.max.y + 5f, ground.center.z);
            fogPlane.transform.localScale = Vector3.one * (mapSize * 6f / 10f);
            var fogRenderer = fogPlane.GetComponent<MeshRenderer>();
            fogRenderer.sharedMaterial = fogMaterial;
            fogRenderer.shadowCastingMode = ShadowCastingMode.Off;
            fogRenderer.receiveShadows = false;
            fog = fogPlane.AddComponent<FogOfWar>();
            fog.demo = demo;
            fog.revealRadius = mapSize * 0.18f;

            return demo;
        }

        static Vector3 FindSpawnPoint(Bounds ground, Bounds everything)
        {
            Physics.SyncTransforms();
            for (float radius = 0f; radius < ground.extents.x; radius += 1f)
            {
                for (float angle = 0f; angle < 360f; angle += radius > 0f ? 30f : 360f)
                {
                    var xz = ground.center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    var point = GroundPoint(xz, everything);
                    var blockers = Physics.OverlapCapsule(point + Vector3.up * 0.9f, point + Vector3.up * 2.5f, 0.8f)
                        .Where(c => !GroundColliders.Contains(c));
                    if (!blockers.Any())
                        return point;
                }
            }
            return GroundPoint(ground.center, everything);
        }

        static Vector3 GroundPoint(Vector3 xz, Bounds everything)
        {
            Physics.SyncTransforms();
            var from = new Vector3(xz.x, everything.max.y + 10f, xz.z);
            var ray = new Ray(from, Vector3.down);
            float best = float.MaxValue;
            Vector3? point = null;
            foreach (var collider in GroundColliders)
            {
                if (collider.Raycast(ray, out var hit, everything.size.y + 20f) && hit.distance < best)
                {
                    best = hit.distance;
                    point = hit.point;
                }
            }
            if (point.HasValue)
                return point.Value;
            Debug.LogWarning($"MAP: no ground found below {xz}");
            return new Vector3(xz.x, everything.center.y, xz.z);
        }

        public static void AddAtmosphereToOpenScene()
        {
            var iso = Object.FindAnyObjectByType<IsometricCamera>();
            var groundRenderers = Object.FindObjectsByType<MeshRenderer>()
                .Where(r => MeshPath(r).Contains("/Models/Lands/"))
                .ToArray();
            if (iso == null || groundRenderers.Length == 0)
                throw new Exception("The open scene needs an IsometricCamera and the location's ground.");
            AddAtmosphere(iso, Encapsulate(groundRenderers));
        }

        static void AddAtmosphere(IsometricCamera iso, Bounds ground)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.74f, 0.84f);
            RenderSettings.fogStartDistance = iso.Distance * 0.85f;
            RenderSettings.fogEndDistance = iso.Distance + iso.mapSize * 1.6f;

            var old = GameObject.Find("Ambient Mist");
            if (old != null)
                Object.DestroyImmediate(old);

            var mist = new GameObject("Ambient Mist").AddComponent<ParticleSystem>();
            var lighting = GameObject.Find("Lighting");
            if (lighting != null)
                mist.transform.SetParent(lighting.transform, false);
            mist.transform.position = new Vector3(ground.center.x, ground.max.y + 1.2f, ground.center.z);
            mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = mist.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.9f, 0.94f, 1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;

            var emission = mist.emission;
            emission.rateOverTime = 7f;

            var shape = mist.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(ground.size.x, 1.5f, ground.size.z);
            shape.randomDirectionAmount = 1f;

            var color = mist.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.14f, 0.3f), new GradientAlphaKey(0.14f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var rotation = mist.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

            var renderer = mist.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 2f;

            mist.Play();
        }

        public static PlayerStart AddPlayerStart(CharacterSelectDemo demo, Transform parent)
        {
            var existing = Object.FindAnyObjectByType<PlayerStart>();
            if (existing != null)
            {
                demo.playerStart = existing;
                return existing;
            }

            var start = new GameObject("Player Start").AddComponent<PlayerStart>();
            start.transform.SetParent(parent, false);
            start.transform.SetPositionAndRotation(demo.spawnPoint, Quaternion.Euler(0f, 225f, 0f));
            demo.playerStart = start;
            EditorUtility.SetDirty(demo);
            return start;
        }

        public static void AddEnemiesToOpenScene()
        {
            var demo = Object.FindAnyObjectByType<CharacterSelectDemo>();
            if (demo == null)
                throw new Exception("The open scene has no CharacterSelectDemo.");

            GroundColliders.Clear();
            var renderers = Object.FindObjectsByType<MeshRenderer>();
            foreach (var r in renderers.Where(r => MeshPath(r).Contains("/Models/Lands/")))
                if (r.TryGetComponent<MeshCollider>(out var collider))
                    GroundColliders.Add(collider);

            PlaceEnemies(demo, Encapsulate(renderers));
        }

        static void PlaceEnemies(CharacterSelectDemo demo, Bounds everything)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab == null)
                return;

            var old = GameObject.Find("Enemies");
            if (old != null)
                Object.DestroyImmediate(old);
            var group = new GameObject("Enemies").transform;

            Physics.SyncTransforms();
            int placed = 0;
            for (float radius = 5f; radius <= 12f && placed < EnemyCount; radius += 2.5f)
            {
                for (float angle = radius * 17f; angle < radius * 17f + 360f && placed < EnemyCount; angle += 55f)
                {
                    var xz = demo.spawnPoint + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                    if (!demo.playArea.Contains(new Vector2(xz.x, xz.z)))
                        continue;

                    var point = GroundPoint(xz, everything);
                    bool blocked = Physics.OverlapCapsule(point + Vector3.up * 0.7f, point + Vector3.up * 1.7f, 0.7f)
                        .Any(c => !GroundColliders.Contains(c));
                    if (blocked)
                        continue;

                    var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                    enemy.name = $"DummyEnemy {placed + 1}";
                    enemy.transform.SetPositionAndRotation(point, Quaternion.LookRotation(demo.spawnPoint - point, Vector3.up));
                    var euler = enemy.transform.eulerAngles;
                    enemy.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
                    Physics.SyncTransforms();
                    placed++;
                }
            }
            Debug.Log($"MAP: placed {placed} dummy enemies");
        }

        static void SetupUi(CharacterSelectDemo demo)
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hudPrefab == null)
                throw new Exception($"UI prefab not found: {HudPrefabPath}");

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hud.GetComponent<GameHUD>().demo = demo;

            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();
        }

        static void SavePreviews(Camera camera, FogOfWar fog, Vector3 spawn, Bounds everything)
        {
            string folder = Path.Combine(Application.dataPath, "../Logs");
            Directory.CreateDirectory(folder);

            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(CharacterBuilder.DefaultRosterPath);
            var temp = new GameObject("PreviewCharacters");
            for (int i = 0; i < roster.entries.Count && i < 2; i++)
            {
                if (roster.entries[i].prefab == null)
                    continue;
                var c = (GameObject)PrefabUtility.InstantiatePrefab(roster.entries[i].prefab.gameObject, temp.transform);
                c.transform.position = GroundPoint(spawn + new Vector3(i * 3f - 1.5f, 0f, 0f), everything);
                c.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
                SampleIdle(c);
            }

            var iso = camera.GetComponent<IsometricCamera>();
            var canvas = Object.FindAnyObjectByType<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            fog.Reveal(spawn);
            iso.FocusOn(spawn);
            Render(camera, Path.Combine(folder, "DemoMap_game.png"));
            fog.Hide();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;

            iso.Fit();
            Render(camera, Path.Combine(folder, "DemoMap_nofog.png"));

            iso.enabled = false;
            camera.orthographic = false;
            camera.fieldOfView = 35f;
            camera.transform.position = spawn + new Vector3(0f, 2.5f, -7f);
            camera.transform.LookAt(spawn + Vector3.up * 1f);
            foreach (Transform c in temp.transform)
                c.rotation = Quaternion.Euler(0f, 180f, 0f);
            Render(camera, Path.Combine(folder, "DemoMap_characters.png"));
            iso.enabled = true;
            iso.Fit();

            Object.DestroyImmediate(temp);
        }

        static void SampleIdle(GameObject character)
        {
            var def = character.GetComponent<HeroCharacter>().definition;
            var visual = character.transform.Find(HeroCharacter.VisualChildName);
            var root = def.idleClip != null && visual != null ? CharacterBuilder.FindAnimationRoot(visual, def.idleClip) : null;
            if (root != null)
                def.idleClip.SampleAnimation(root.gameObject, 0.5f);
        }

        static void Render(Camera camera, string file)
        {
            var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            File.WriteAllBytes(file, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        static Bounds Encapsulate(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }
}
