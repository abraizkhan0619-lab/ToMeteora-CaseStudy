using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ToMeteora.Tests
{
    public class CharacterSmokeTests
    {
        [UnityTest]
        public IEnumerator Character_CanBeSelected_AndSpawned(
            [Values("hero_angelo", "hero_brick")] string id)
        {
            var demo = new GameObject("Demo").AddComponent<CharacterSelectDemo>();
            demo.roster = Resources.Load<CharacterRoster>("CharacterRoster");
            yield return null;

            var hero = demo.Select(id);
            yield return null;

            Assert.That(hero, Is.Not.Null, $"Could not spawn '{id}'");
            Assert.That(demo.Current, Is.EqualTo(hero));
            Assert.That(hero.definition.id, Is.EqualTo(id));
            Assert.That(hero.CurrentHealth, Is.EqualTo(hero.definition.health));
            Assert.That(hero.UseAbility(), Is.True);
            Assert.That(hero.Ability.CooldownRemaining, Is.EqualTo(hero.definition.cooldown).Within(0.1f));

            Object.Destroy(demo.gameObject);
            Object.Destroy(hero.gameObject);
        }
    }
}
