using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ToMeteora.Tools
{
    public class CharacterAutoRegister : AssetPostprocessor
    {
        public const string CharacterFolder = "Assets/ToMeteora/Data/Characters/";

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var paths = imported.Concat(moved)
                .Where(p => p.StartsWith(CharacterFolder) && p.EndsWith(".asset"))
                .ToList();
            bool removed = deleted.Concat(movedFrom).Any(p => p.StartsWith(CharacterFolder));
            if (paths.Count == 0 && !removed)
                return;

            EditorApplication.delayCall += () => Register(paths, removed);
        }

        static void Register(List<string> paths, bool cleanUp)
        {
            var roster = CharacterBuilder.LoadOrCreateRoster(CharacterBuilder.DefaultRosterPath);
            bool changed = false;

            foreach (var path in paths)
            {
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (def == null)
                    continue;

                var entry = roster.FindByDefinition(def);
                if (entry == null)
                {
                    roster.entries.Add(new CharacterRoster.Entry { definition = def });
                    changed = true;
                }
                else if (entry.prefab != null && entry.prefab.IsReady)
                    continue;

                SaveRoster(roster, ref changed);
                var errors = CharacterValidator.Validate(def);
                if (errors.Count == 0)
                {
                    CharacterBuilder.CreateOrUpdate(def);
                    Debug.Log($"'{def.name}' is in the roster and its prefab is built.");
                }
                else if (CharacterBuilder.CreatePlaceholder(def).Count == 0)
                {
                    Debug.Log($"'{def.name}' is in the roster with a placeholder prefab. It is finished on Create / Update once valid: {errors[0]}");
                }
                else
                {
                    Debug.Log($"'{def.name}' was added to the roster. Its prefab is created once it has a valid ID: {errors[0]}");
                }
            }

            if (cleanUp)
                changed |= roster.entries.RemoveAll(e => e.definition == null) > 0;
            SaveRoster(roster, ref changed);
        }

        static void SaveRoster(CharacterRoster roster, ref bool changed)
        {
            if (!changed)
                return;
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssetIfDirty(roster);
            changed = false;
        }
    }
}
