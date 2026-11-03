using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToMeteora
{
    public class CharacterRoster : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public CharacterDefinition definition;
            public HeroCharacter prefab;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry FindById(string id) =>
            entries.Find(e => e.definition != null && e.definition.id == id);

        public Entry FindByDefinition(CharacterDefinition definition) =>
            entries.Find(e => e.definition == definition);
    }
}
