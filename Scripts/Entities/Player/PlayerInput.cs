using UnityEngine;
using Foundation.Events;

namespace Entities.Player
{
    /// <summary>
    /// Handles player input and publishes input events
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float inputDeadzone = 0.1f;

        // Input state
        private Vector2 movementInput;
        private bool jumpPressed;
        private bool interactPressed;
        private bool inventoryPressed;
        private bool attackPressed;

        // Components
        private PlayerController playerController;

        // Properties
        public bool InputEnabled => enableInput;
        public Vector2 MovementInput => movementInput;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;
        }

        private void Update()
        {
            if (!enableInput) return;

            HandleMovementInput();
            HandleActionInputs();
        }

        private void HandleMovementInput()
        {
            // Get movement input
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            movementInput = new Vector2(horizontal, vertical);

            // Apply deadzone
            if (movementInput.magnitude < inputDeadzone)
            {
                movementInput = Vector2.zero;
            }

            // Publish movement event if there's input
            if (movementInput != Vector2.zero)
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.Movement, movementInput));
            }
        }

        private void HandleActionInputs()
        {
            // Jump input
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.Jump));
            }

            // Interact input
            if (Input.GetKeyDown(KeyCode.E))
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.Interact));
            }

            // Inventory input
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.OpenInventory));
            }

            // Attack input
            if (Input.GetMouseButtonDown(0))
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.Attack));
            }

            // Use item input
            if (Input.GetKeyDown(KeyCode.Q))
            {
                EventBus.Publish(new PlayerInputEvent(PlayerInputType.UseItem));
            }
        }

        /// <summary>
        /// Enable or disable input
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            enableInput = enabled;
            
            if (!enabled)
            {
                // Clear current input when disabled
                movementInput = Vector2.zero;
            }
        }

        /// <summary>
        /// Set input deadzone
        /// </summary>
        public void SetDeadzone(float deadzone)
        {
            inputDeadzone = Mathf.Clamp01(deadzone);
        }

        #region Input Actions (for New Input System integration)
        
        // These methods can be called by Unity's new Input System if used
        
        public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput) return;
            
            movementInput = context.ReadValue<Vector2>();
            
            if (movementInput.magnitude < inputDeadzone)
            {
                movementInput = Vector2.zero;
            }
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.Movement, movementInput));
        }

        public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput || !context.performed) return;
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.Jump));
        }

        public void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput || !context.performed) return;
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.Interact));
        }

        public void OnInventory(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput || !context.performed) return;
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.OpenInventory));
        }

        public void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput || !context.performed) return;
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.Attack));
        }

        public void OnUseItem(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!enableInput || !context.performed) return;
            
            EventBus.Publish(new PlayerInputEvent(PlayerInputType.UseItem));
        }
        
        #endregion
    }
}
