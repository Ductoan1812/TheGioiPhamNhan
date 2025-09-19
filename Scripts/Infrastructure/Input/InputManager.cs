using UnityEngine;
using Foundation.Events;
using UnityInput = UnityEngine.Input;

namespace Infrastructure.Input
{
    /// <summary>
    /// Central input manager for the game
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float inputDeadzone = 0.1f;

        // Input state
        private bool inputEnabled = true;

        // Properties
        public bool InputEnabled => inputEnabled && enableInput;

        public void Initialize()
        {
            Debug.Log("Input Manager initialized");
        }

        private void Update()
        {
            if (!InputEnabled) return;

            // Handle global input (like pause)
            HandleGlobalInput();
        }

        private void HandleGlobalInput()
        {
            // Escape key for pause/menu
            if (UnityInput.GetKeyDown(KeyCode.Escape))
            {
                EventBus.Publish(new GlobalInputEvent(GlobalInputType.Escape));
            }

            // Other global inputs can be added here
        }

        /// <summary>
        /// Enable or disable all input
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        /// <summary>
        /// Set input deadzone
        /// </summary>
        public void SetDeadzone(float deadzone)
        {
            inputDeadzone = Mathf.Clamp01(deadzone);
        }

        /// <summary>
        /// Check if input is enabled
        /// </summary>
        public bool IsInputEnabled()
        {
            return InputEnabled;
        }
    }

    /// <summary>
    /// Global input types
    /// </summary>
    public enum GlobalInputType
    {
        Escape,
        Menu,
        Screenshot,
        Console
    }

    /// <summary>
    /// Global input event
    /// </summary>
    public class GlobalInputEvent : GameEvent<GlobalInputType>
    {
        public GlobalInputType InputType => Data;

        public GlobalInputEvent(GlobalInputType inputType) : base(inputType)
        {
        }
    }
}
