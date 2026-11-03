using UnityEngine;

namespace ToMeteora
{
    public class CharacterSelectDemo : MonoBehaviour
    {
        public CharacterRoster roster;
        [Tooltip("Where characters appear.")]
        public Vector3 spawnPoint = Vector3.zero;
        [Tooltip("Area (x/z) the character must stay inside. Width or height 0 = no limit.")]
        public Rect playArea;

        public HeroCharacter Current { get; private set; }

        void Awake()
        {
            if (roster == null)
                roster = Resources.Load<CharacterRoster>("CharacterRoster");
        }

        void Start()
        {
            if (Current != null || roster == null)
                return;
            var first = roster.entries.Find(e => e.definition != null && e.prefab != null);
            if (first != null)
                Select(first.definition.id);
        }

        public HeroCharacter Select(string id)
        {
            var entry = roster.FindById(id);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError($"No ready character with ID '{id}' in the roster. Run To Meteora > Character Setup.");
                return null;
            }

            var position = Current != null ? Current.transform.position : spawnPoint;
            var rotation = Current != null ? Current.transform.rotation : Quaternion.identity;
            if (Current != null)
                Destroy(Current.gameObject);

            Current = Instantiate(entry.prefab, position, rotation);
            Current.moveBounds = playArea;
            return Current;
        }

        void Update()
        {
            if (Current == null)
                return;

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            var cam = Camera.main;
            if (cam != null)
                input = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f) * input;
            Current.Move(input, Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
                Current.UseAbility();
        }
    }
}
