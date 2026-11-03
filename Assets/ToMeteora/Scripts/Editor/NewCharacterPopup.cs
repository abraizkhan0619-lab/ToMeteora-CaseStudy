using System;
using UnityEditor;
using UnityEngine;

namespace ToMeteora.Tools
{
    public class NewCharacterPopup : PopupWindowContent
    {
        readonly Action<CharacterDefinition> created;
        string id = "hero_";
        string displayName = "";
        bool focused;

        public NewCharacterPopup(Action<CharacterDefinition> created) => this.created = created;

        public override Vector2 GetWindowSize() => new Vector2(320f, 190f);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("New Character", EditorStyles.boldLabel);

            GUI.SetNextControlName("id");
            id = EditorGUILayout.TextField("ID", id).Trim();
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            if (!focused)
            {
                EditorGUI.FocusTextInControl("id");
                focused = true;
            }

            var errors = CharacterValidator.ValidateId(id);
            if (errors.Count > 0)
                EditorGUILayout.HelpBox(errors[0], MessageType.Warning);
            else
                EditorGUILayout.HelpBox($"Creates Data/Characters/{FileName()}.asset and Prefabs/Characters/{id}.prefab.", MessageType.Info);

            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("Create", GUILayout.Height(28f)))
                    Create();
            }
        }

        string FileName() => string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim().Replace(" ", "");

        void Create()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{CharacterAutoRegister.CharacterFolder}{FileName()}.asset");
            var def = ScriptableObject.CreateInstance<CharacterDefinition>();
            def.id = id;
            def.displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            def.ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ToMeteora/Data/Abilities/BasicAttack.asset");
            CharacterBuilder.EnsureFolder(CharacterAutoRegister.CharacterFolder.TrimEnd('/'));
            AssetDatabase.CreateAsset(def, path);

            foreach (var error in CharacterBuilder.CreatePlaceholder(def))
                Debug.LogError(error);

            created?.Invoke(def);
            editorWindow.Close();
        }
    }
}
