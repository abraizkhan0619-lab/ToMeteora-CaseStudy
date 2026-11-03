using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToMeteora
{
    public class GameHUD : MonoBehaviour
    {
        public CharacterSelectDemo demo;

        [Header("Character list")]
        [Tooltip("Buttons for each character are created here.")]
        public Transform characterButtonParent;
        [Tooltip("A hidden button in the prefab that is copied once per character. Style it in the prefab.")]
        public Button characterButtonTemplate;

        [Header("Selected character")]
        public TMP_Text nameText;
        public TMP_Text idText;
        public TMP_Text healthText;
        public TMP_Text speedText;
        public TMP_Text abilityText;
        public TMP_Text cooldownText;
        public Image healthFill;
        public Image cooldownFill;
        public Button attackButton;

        void Start()
        {
            if (demo == null)
                demo = FindAnyObjectByType<CharacterSelectDemo>();

            characterButtonTemplate.gameObject.SetActive(false);
            foreach (var entry in demo.roster.entries)
            {
                if (entry.definition == null || entry.prefab == null)
                    continue;

                var button = Instantiate(characterButtonTemplate, characterButtonParent);
                button.gameObject.SetActive(true);
                button.name = entry.definition.id;
                button.GetComponentInChildren<TMP_Text>().text = entry.definition.displayName;
                string id = entry.definition.id;
                button.onClick.AddListener(() => { demo.Select(id); ClearUiFocus(); });
            }

            if (attackButton != null)
                attackButton.onClick.AddListener(() => { if (demo.Current != null) demo.Current.UseAbility(); ClearUiFocus(); });
        }

        void Update()
        {
            var hero = demo != null ? demo.Current : null;
            if (hero == null)
                return;

            var d = hero.definition;
            nameText.text = d.displayName;
            idText.text = d.id;
            healthText.text = $"{hero.CurrentHealth:0} / {d.health:0}";
            speedText.text = $"{d.moveSpeed} u/s";
            abilityText.text = d.ability.displayName;
            if (healthFill != null)
                healthFill.fillAmount = hero.CurrentHealth / d.health;

            float remaining = hero.Ability.CooldownRemaining;
            cooldownText.text = remaining > 0f ? $"{remaining:0.0}s" : "Ready";
            cooldownFill.fillAmount = 1f - remaining / d.cooldown;
        }

        static void ClearUiFocus()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
