using Genesis.Character;
using Genesis.Items;
using UnityEngine;

namespace Genesis.Combat
{
    /// <summary>Plays sword presentation clips while preserving the locomotion controller.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActorInventory), typeof(PlayerMovementInput))]
    public sealed class PlayerSwordCombat : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private ActorInventory inventory;
        [SerializeField] private PlayerMovementInput movementInput;
        [SerializeField] private KeyCode heavyAttackKey = KeyCode.F;

        private static readonly int HasSword = Animator.StringToHash("HasSword");
        private static readonly int LightAttack = Animator.StringToHash("SwordLightAttack");
        private static readonly int LightComboIndex = Animator.StringToHash("SwordLightComboIndex");
        private static readonly int HeavyAttack = Animator.StringToHash("SwordHeavyAttack");
        private static readonly int Blocking = Animator.StringToHash("SwordBlocking");
        private int _nextComboIndex;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (inventory == null) inventory = GetComponent<ActorInventory>();
            if (movementInput == null) movementInput = GetComponent<PlayerMovementInput>();
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null || inventory?.Equipment == null)
                return;

            ItemDefinition mainHand = inventory.Equipment.Get(EquipmentSlot.MainHand);
            bool armed = mainHand != null && mainHand.Category == ItemCategory.Weapon;
            animator.SetBool(HasSword, armed);

            bool inputSuspended = movementInput != null && movementInput.Suspended;
            bool blocking = armed && !inputSuspended && Input.GetMouseButton(1);
            animator.SetBool(Blocking, blocking);
            if (!armed || inputSuspended || blocking) return;

            // Fire1 is also the sandbox's practice action, so the swing and practice
            // feedback happen together when the player clicks the training dummy.
            if (Input.GetButtonDown("Fire1"))
            {
                animator.SetInteger(LightComboIndex, _nextComboIndex);
                animator.ResetTrigger(LightAttack);
                animator.SetTrigger(LightAttack);
                _nextComboIndex = (_nextComboIndex + 1) % 3;
            }

            if (Input.GetKeyDown(heavyAttackKey))
            {
                animator.ResetTrigger(HeavyAttack);
                animator.SetTrigger(HeavyAttack);
            }
        }
    }
}
