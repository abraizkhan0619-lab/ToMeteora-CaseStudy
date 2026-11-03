using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ToMeteora.Tools
{
    public class CharacterSetupWindow : EditorWindow
    {
        enum PreviewClip { Idle, Move, Attack }

        static readonly string[] PreviewClipNames = { "Idle", "Move", "Attack" };

        List<CharacterDefinition> definitions;
        CharacterDefinition selected;
        Editor definitionEditor;
        Editor abilityEditor;
        bool showAbility = true;
        readonly List<string> errors = new List<string>();
        string resultMessage;
        Vector2 listScroll;
        Vector2 detailScroll;

        CharacterPreview preview;
        PreviewClip previewClip = PreviewClip.Idle;
        bool playing = true;
        float previewTime;
        double lastTickTime;

        [MenuItem("To Meteora/Character Setup")]
        static void Open()
        {
            var window = GetWindow<CharacterSetupWindow>("Character Setup");
            window.minSize = new Vector2(820f, 560f);
        }

        void OnEnable()
        {
            lastTickTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            preview?.Dispose();
            preview = null;
            if (definitionEditor != null)
                DestroyImmediate(definitionEditor);
            if (abilityEditor != null)
                DestroyImmediate(abilityEditor);
        }

        void OnProjectChange() => definitions = null;

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing && selected != null && CanPreview(selected))
            {
                previewTime += (float)(now - lastTickTime);
                Repaint();
            }
            lastTickTime = now;
        }

        void OnGUI()
        {
            definitions ??= CharacterValidator.FindAllDefinitions();
            if (selected == null && definitions.Count > 0)
                Select(definitions[0]);

            EditorGUILayout.BeginHorizontal();
            DrawCharacterList();
            DrawDetails();
            EditorGUILayout.EndHorizontal();
        }

        void DrawCharacterList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200f));
            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (var def in definitions)
            {
                if (def == null)
                    continue;
                string label = string.IsNullOrEmpty(def.displayName) ? def.name : def.displayName;
                bool isSelected = def == selected;
                if (GUILayout.Toggle(isSelected, $"{label}\n<size=10>{def.id}</size>", ListItemStyle, GUILayout.Height(38f)) && !isSelected)
                    Select(def);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Character...", GUILayout.Height(26f)))
                PopupWindow.Show(GUILayoutUtility.GetLastRect(), new NewCharacterPopup(OnCharacterCreated));
            if (GUILayout.Button("New Ability...", GUILayout.Height(26f)))
                ShowNewAbilityMenu();
            if (GUILayout.Button("Create / Update All", GUILayout.Height(26f)))
                CreateOrUpdateAll();
            EditorGUILayout.EndVertical();
        }

        void DrawDetails()
        {
            if (selected == null)
            {
                EditorGUILayout.HelpBox("No characters yet. Click 'New Character...' to make one.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(300f));
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            Editor.CreateCachedEditor(selected, null, ref definitionEditor);
            EditorGUI.BeginChangeCheck();
            definitionEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                errors.Clear();
                resultMessage = null;
            }
            DrawAbilitySettings();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(330f));
            DrawPreview();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            DrawActions();
            EditorGUILayout.EndVertical();
        }

        void DrawAbilitySettings()
        {
            if (selected.ability == null)
                return;

            EditorGUILayout.Space(8f);
            showAbility = EditorGUILayout.Foldout(showAbility, $"Ability settings: {selected.ability.name}", true, EditorStyles.foldoutHeader);
            if (!showAbility)
                return;

            int users = definitions.Count(d => d != null && d.ability == selected.ability);
            if (users > 1)
                EditorGUILayout.HelpBox($"This ability is shared by {users} characters. Changes apply to all of them.", MessageType.None);

            Editor.CreateCachedEditor(selected.ability, null, ref abilityEditor);
            abilityEditor.OnInspectorGUI();
        }

        void ShowNewAbilityMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in TypeCache.GetTypesDerivedFrom<AbilityDefinition>().Where(t => !t.IsAbstract))
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name.Replace("Definition", ""))), false, () => CreateNewAbility(type));
            menu.ShowAsContext();
        }

        void CreateNewAbility(System.Type type)
        {
            string path = EditorUtility.SaveFilePanelInProject("New Ability", "NewAbility", "asset",
                "Where should the ability be saved?", "Assets/ToMeteora/Data/Abilities");
            if (string.IsNullOrEmpty(path))
                return;

            var ability = (AbilityDefinition)CreateInstance(type);
            ability.displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            var template = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ToMeteora/Data/Abilities/BasicAttack.asset");
            if (template != null)
                ability.areaIndicator = template.areaIndicator;
            AssetDatabase.CreateAsset(ability, path);

            if (selected != null && EditorUtility.DisplayDialog("New Ability",
                    $"Use '{ability.name}' for {selected.displayName}?", "Yes", "No"))
            {
                Undo.RecordObject(selected, "Assign ability");
                selected.ability = ability;
                EditorUtility.SetDirty(selected);
                AssetDatabase.SaveAssetIfDirty(selected);
            }
            EditorGUIUtility.PingObject(ability);
            resultMessage = $"Created ability '{ability.name}' at {path}.";
        }

        void DrawActions()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate", GUILayout.Height(30f)))
                Validate();
            if (GUILayout.Button("Create / Update", GUILayout.Height(30f)))
                CreateOrUpdate();
            if (HasNonLoopingClips(selected) && GUILayout.Button("Make Clips Loop", GUILayout.Height(30f)))
            {
                CharacterBuilder.MakeClipsLoop(selected);
                Validate();
            }
            EditorGUILayout.EndHorizontal();

            foreach (var error in errors)
                EditorGUILayout.HelpBox(error, MessageType.Error);
            if (resultMessage != null)
                EditorGUILayout.HelpBox(resultMessage, MessageType.Info);
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(330f, 330f, GUILayout.ExpandWidth(true));

            if (!CanPreview(selected))
            {
                EditorGUI.HelpBox(rect, "Assign a Visual Prefab with a mesh and a Visual Height above 0 to see the character.", MessageType.Info);
                return;
            }

            preview ??= new CharacterPreview();
            preview.Show(selected);
            HandleOrbit(rect);

            if (Event.current.type == EventType.Repaint)
            {
                preview.Sample(ClipToPreview(), previewTime);
                preview.ShowEffect(selected, previewClip == PreviewClip.Attack, previewTime);
                preview.Draw(rect);
            }

            previewClip = (PreviewClip)GUILayout.Toolbar((int)previewClip, PreviewClipNames);

            var clip = CurrentClip();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(playing ? "Pause" : "Play", GUILayout.Width(60f)))
                playing = !playing;
            if (clip != null)
            {
                float time = clip.length > 0f ? previewTime % clip.length : 0f;
                float scrubbed = EditorGUILayout.Slider(time, 0f, clip.length);
                if (!Mathf.Approximately(scrubbed, time))
                {
                    previewTime = scrubbed;
                    playing = false;
                }
            }
            else
            {
                EditorGUILayout.LabelField(previewClip == PreviewClip.Attack ? "No attack clip: showing idle with the attack effect." : "No clip assigned.", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Drag the preview to rotate.", EditorStyles.centeredGreyMiniLabel);
        }

        void HandleOrbit(Rect rect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                preview.orbit.x += e.delta.x * 0.6f;
                preview.orbit.y = Mathf.Clamp(preview.orbit.y + e.delta.y * 0.4f, -10f, 80f);
                e.Use();
                Repaint();
            }
        }

        AnimationClip CurrentClip() => previewClip switch
        {
            PreviewClip.Move => selected.moveClip,
            PreviewClip.Attack => selected.attackClip,
            _ => selected.idleClip,
        };

        AnimationClip ClipToPreview()
        {
            var clip = CurrentClip();
            return clip != null ? clip : selected.idleClip;
        }

        void Select(CharacterDefinition def)
        {
            selected = def;
            errors.Clear();
            resultMessage = null;
            previewTime = 0f;
            preview?.Clear();
            GUI.FocusControl(null);
        }

        void Validate()
        {
            errors.Clear();
            errors.AddRange(CharacterValidator.Validate(selected));
            resultMessage = errors.Count == 0 ? $"'{selected.id}' is valid." : null;
        }

        void CreateOrUpdate()
        {
            errors.Clear();
            errors.AddRange(CharacterBuilder.CreateOrUpdate(selected));
            resultMessage = errors.Count == 0
                ? $"'{selected.id}' is ready: {CharacterBuilder.PrefabPathFor(selected, CharacterBuilder.DefaultOutputFolder)} (registered in roster)."
                : null;
        }

        void CreateOrUpdateAll()
        {
            errors.Clear();
            errors.AddRange(CharacterBuilder.CreateOrUpdateAll());
            resultMessage = errors.Count == 0 ? "All characters are ready." : null;
        }

        void OnCharacterCreated(CharacterDefinition def)
        {
            definitions = null;
            Select(def);
            resultMessage = $"Created '{def.id}' and {CharacterBuilder.PrefabPathFor(def, CharacterBuilder.DefaultOutputFolder)}. Assign a Visual Prefab and clips, then click Create / Update.";
            Repaint();
        }

        static bool CanPreview(CharacterDefinition def) =>
            def.visualPrefab != null && def.visualHeight > 0f && def.visualPrefab.GetComponentInChildren<Renderer>() != null;

        static bool HasNonLoopingClips(CharacterDefinition def) =>
            (def.idleClip != null && !CharacterValidator.Loops(def.idleClip)) ||
            (def.moveClip != null && !CharacterValidator.Loops(def.moveClip));

        static GUIStyle listItemStyle;
        static GUIStyle ListItemStyle => listItemStyle ??= new GUIStyle(EditorStyles.toolbarButton)
        {
            alignment = TextAnchor.MiddleLeft,
            richText = true,
            fixedHeight = 0f,
            padding = new RectOffset(8, 8, 4, 4),
        };
    }
}
