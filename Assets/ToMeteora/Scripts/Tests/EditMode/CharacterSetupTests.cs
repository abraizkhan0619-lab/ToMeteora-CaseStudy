using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ToMeteora.Tools;
using UnityEditor;
using UnityEngine;

namespace ToMeteora.Tests
{
    public class CharacterSetupTests
    {
        const string TempFolder = "Assets/ToMeteora/_TestTemp";
        const string OutputFolder = TempFolder + "/Generated";

        GameObject visual;
        AbilityDefinition ability;
        CharacterRoster roster;

        [SetUp]
        public void SetUp()
        {
            CharacterBuilder.EnsureFolder(TempFolder);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual = PrefabUtility.SaveAsPrefabAsset(cube, TempFolder + "/Visual.prefab");
            Object.DestroyImmediate(cube);

            ability = ScriptableObject.CreateInstance<BasicAttackDefinition>();
            AssetDatabase.CreateAsset(ability, TempFolder + "/Ability.asset");
            roster = CharacterBuilder.LoadOrCreateRoster(TempFolder + "/Roster.asset");
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TempFolder);

        [Test]
        public void ValidDefinition_CreatesPrefab_AndRegistersIt()
        {
            var def = MakeDefinition("Alpha", "test_alpha", visual);

            Assert.That(Build(def), Is.Empty);

            var prefab = AssetDatabase.LoadAssetAtPath<HeroCharacter>(PrefabPath(def));
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.definition, Is.EqualTo(def));
            Assert.That(prefab.GetComponent<BasicAttack>(), Is.Not.Null);
            Assert.That(roster.FindById("test_alpha").prefab, Is.EqualTo(prefab));
        }

        [Test]
        public void RunningSetupTwice_CreatesNoDuplicates_AndKeepsReferences()
        {
            var def = MakeDefinition("Alpha", "test_alpha", visual);
            Build(def);
            string guid = AssetDatabase.AssetPathToGUID(PrefabPath(def));
            string text = File.ReadAllText(PrefabPath(def));

            Assert.That(Build(def), Is.Empty);

            Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath(def)), Is.EqualTo(guid));
            Assert.That(File.ReadAllText(PrefabPath(def)), Is.EqualTo(text));
            Assert.That(AssetDatabase.FindAssets("t:Prefab", new[] { OutputFolder }).Length, Is.EqualTo(1));
            Assert.That(roster.entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void IntentionalUpdate_ToSameDefinition_StaysValid()
        {
            var def = MakeDefinition("Alpha", "test_alpha", visual);
            Build(def);
            string guid = AssetDatabase.AssetPathToGUID(PrefabPath(def));

            def.health = 250f;

            Assert.That(Build(def), Is.Empty);
            Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath(def)), Is.EqualTo(guid));
            Assert.That(roster.entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void MissingVisual_IsRejected_AndNothingIsCreated()
        {
            var def = MakeDefinition("Alpha", "test_alpha", null);

            Assert.That(Build(def), Has.Some.Contains("Visual Prefab is missing"));
            AssertNothingCreated(def);
        }

        [Test]
        public void TwoDefinitionsWithSameId_AreRejected_AndNothingIsCreated()
        {
            var first = MakeDefinition("Alpha", "test_dupe", visual);
            var second = MakeDefinition("Beta", "test_dupe", visual);

            Assert.That(Build(first), Has.Some.Contains("ID 'test_dupe' is also used by"));
            Assert.That(Build(second), Has.Some.Contains("ID 'test_dupe' is also used by"));
            AssertNothingCreated(first);
            AssertNothingCreated(second);
        }

        [Test]
        public void AddingBrick_DoesNotChangeAngelo()
        {
            var angelo = FindById("hero_angelo");
            var brick = FindById("hero_brick");
            string[] angeloFiles =
            {
                AssetDatabase.GetAssetPath(angelo),
                CharacterBuilder.PrefabPathFor(angelo, CharacterBuilder.DefaultOutputFolder),
            };
            var before = new string[angeloFiles.Length];
            for (int i = 0; i < angeloFiles.Length; i++)
                before[i] = File.ReadAllText(angeloFiles[i]);
            var roster = CharacterBuilder.LoadOrCreateRoster(CharacterBuilder.DefaultRosterPath);
            var angeloPrefab = roster.FindById("hero_angelo").prefab;

            Assert.That(CharacterBuilder.CreateOrUpdate(brick), Is.Empty);

            for (int i = 0; i < angeloFiles.Length; i++)
                Assert.That(File.ReadAllText(angeloFiles[i]), Is.EqualTo(before[i]), $"{angeloFiles[i]} changed");
            Assert.That(roster.FindById("hero_angelo").prefab, Is.EqualTo(angeloPrefab));
            Assert.That(CharacterValidator.Validate(angelo), Is.Empty);
        }

        List<string> Build(CharacterDefinition def) => CharacterBuilder.CreateOrUpdate(def, roster, OutputFolder);

        static string PrefabPath(CharacterDefinition def) => CharacterBuilder.PrefabPathFor(def, OutputFolder);

        static CharacterDefinition FindById(string id) =>
            CharacterValidator.FindAllDefinitions().Find(d => d.id == id);

        void AssertNothingCreated(CharacterDefinition def)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(def)), Is.Null);
            Assert.That(roster.entries, Is.Empty);
        }

        CharacterDefinition MakeDefinition(string file, string id, GameObject visualPrefab)
        {
            var def = ScriptableObject.CreateInstance<CharacterDefinition>();
            def.id = id;
            def.displayName = file;
            def.visualPrefab = visualPrefab;
            def.health = 100;
            def.moveSpeed = 5;
            def.cooldown = 3;
            def.ability = ability;
            AssetDatabase.CreateAsset(def, $"{TempFolder}/{file}.asset");
            return def;
        }
    }
}
