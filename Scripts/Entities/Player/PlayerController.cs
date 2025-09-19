using UnityEngine;
using GameSystems.Stats;
using GameSystems.Inventory;
using Foundation.Events;

namespace Entities.Player
{
    /// <summary>
    /// Main player controller - orchestrates all player components
    /// </summary>
    public class PlayerController : MonoBehaviour, IEventDispatcher
    {
        [Header("Components")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInput playerInput;

        [Header("Stats")]
        [SerializeField] private StatCollection playerStats;

        // Properties
        public PlayerMovement Movement => movement;
        public PlayerHealth Health => health;
        public PlayerInventory PlayerInventory => inventory;
        public StatCollection Stats => playerStats;

        // Events
        public System.Action<Vector3> OnPlayerMoved;
        public System.Action<float> OnHealthChanged;

        private void Awake()
        {
            InitializeComponents();
            InitializeStats();
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Subscribe<PlayerInputEvent>(OnPlayerInputReceived);
            
            // Initialize components
            movement?.Initialize(this);
            health?.Initialize(playerStats);
            inventory?.Initialize();
            playerInput?.Initialize(this);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<PlayerInputEvent>(OnPlayerInputReceived);
        }

        private void InitializeComponents()
        {
            // Get components if not assigned
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();

            // Create stats collection if not assigned
            if (playerStats == null)
            {
                playerStats = new StatCollection();
            }
        }

        private void InitializeStats()
        {
            // Initialize default player stats
            playerStats.AddStat(StatType.Health, 100f);
            playerStats.AddStat(StatType.Mana, 50f);
            playerStats.AddStat(StatType.Stamina, 100f);
            playerStats.AddStat(StatType.Strength, 10f);
            playerStats.AddStat(StatType.Defense, 5f);
            playerStats.AddStat(StatType.Speed, 5f);
            
            playerStats.Initialize();
        }

        private void OnPlayerInputReceived(PlayerInputEvent inputEvent)
        {
            // Handle different input types
            switch (inputEvent.InputType)
            {
                case PlayerInputType.Movement:
                    movement?.HandleMovementInput(inputEvent.InputVector);
                    break;
                    
                case PlayerInputType.Jump:
                    movement?.HandleJumpInput();
                    break;
                    
                case PlayerInputType.Interact:
                    HandleInteraction();
                    break;
                    
                case PlayerInputType.OpenInventory:
                    inventory?.ToggleInventoryUI();
                    break;
            }
        }

        private void HandleInteraction()
        {
            // Raycast or trigger detection for interaction
            var interactable = DetectNearbyInteractable();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
            }
        }

        private IInteractable DetectNearbyInteractable()
        {
            // Simple sphere cast for nearby interactables
            var colliders = Physics.OverlapSphere(transform.position, 2f);
            
            foreach (var collider in colliders)
            {
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    return interactable;
                }
            }
            
            return null;
        }

        public void DispatchEvent<T>(T gameEvent) where T : GameEvent
        {
            EventBus.Publish(gameEvent);
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public void TakeDamage(float damage)
        {
            health?.TakeDamage(damage);
        }

        /// <summary>
        /// Heal player
        /// </summary>
        public void Heal(float amount)
        {
            health?.Heal(amount);
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public bool AddItem(Item item, int quantity = 1)
        {
            return inventory?.AddItem(item, quantity).Success ?? false;
        }

        /// <summary>
        /// Get player position
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Set player position
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            OnPlayerMoved?.Invoke(position);
        }
    }

    /// <summary>
    /// Interface for interactable objects
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }

    /// <summary>
    /// Player input event
    /// </summary>
    public enum PlayerInputType
    {
        Movement,
        Jump,
        Interact,
        Attack,
        OpenInventory,
        UseItem
    }

    public class PlayerInputEvent : GameEvent<PlayerInputData>
    {
        public PlayerInputType InputType => Data.InputType;
        public Vector2 InputVector => Data.InputVector;

        public PlayerInputEvent(PlayerInputType inputType, Vector2 inputVector = default) 
            : base(new PlayerInputData(inputType, inputVector))
        {
        }
    }

    [System.Serializable]
    public class PlayerInputData
    {
        public PlayerInputType InputType { get; }
        public Vector2 InputVector { get; }

        public PlayerInputData(PlayerInputType inputType, Vector2 inputVector)
        {
            InputType = inputType;
            InputVector = inputVector;
        }
    }
}
