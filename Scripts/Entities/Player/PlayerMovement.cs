using UnityEngine;
using Foundation.Events;

namespace Entities.Player
{
    /// <summary>
    /// Handles player movement mechanics
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundMask = 1;

        // Components
        private CharacterController characterController;
        private PlayerController playerController;

        // Movement state
        private Vector3 velocity;
        private bool isGrounded;
        private bool isRunning;
        private Vector2 currentInput;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsMoving => currentInput.magnitude > 0.1f;
        public bool IsRunning => isRunning;
        public float CurrentSpeed => isRunning ? runSpeed : walkSpeed;
        public Vector3 Velocity => characterController.velocity;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;
            characterController = GetComponent<CharacterController>();
            
            // Create ground check if not assigned
            if (groundCheck == null)
            {
                var groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = Vector3.down * (characterController.height / 2f);
                groundCheck = groundCheckObj.transform;
            }
        }

        private void Update()
        {
            GroundCheck();
            ApplyGravity();
            Move();
        }

        private void GroundCheck()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundMask);
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative value to keep grounded
            }
        }

        private void ApplyGravity()
        {
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        private void Move()
        {
            if (currentInput.magnitude > 0.1f)
            {
                // Calculate movement direction
                var moveDirection = transform.right * currentInput.x + transform.forward * currentInput.y;
                moveDirection.Normalize();

                // Apply movement
                var speed = CurrentSpeed;
                characterController.Move(moveDirection * speed * Time.deltaTime);

                // Publish movement event
                EventBus.Publish(new PlayerMovementEvent(moveDirection, speed, IsRunning));
            }

            // Apply vertical movement (gravity/jump)
            characterController.Move(velocity * Time.deltaTime);
        }

        public void HandleMovementInput(Vector2 input)
        {
            currentInput = input;
            
            // Check for running (assuming shift is held, this would come from input system)
            isRunning = Input.GetKey(KeyCode.LeftShift) && input.magnitude > 0.1f;
        }

        public bool HandleJumpInput()
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                EventBus.Publish(new PlayerJumpEvent(transform.position));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set movement speed modifiers
        /// </summary>
        public void SetSpeedModifier(float walkMultiplier, float runMultiplier)
        {
            walkSpeed *= walkMultiplier;
            runSpeed *= runMultiplier;
        }

        /// <summary>
        /// Teleport player to position
        /// </summary>
        public void Teleport(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Add external force (knockback, wind, etc.)
        /// </summary>
        public void AddForce(Vector3 force)
        {
            velocity += force;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
            }
        }
    }

    /// <summary>
    /// Player movement event
    /// </summary>
    public class PlayerMovementEvent : GameEvent<PlayerMovementData>
    {
        public Vector3 Direction => Data.Direction;
        public float Speed => Data.Speed;
        public bool IsRunning => Data.IsRunning;

        public PlayerMovementEvent(Vector3 direction, float speed, bool isRunning) 
            : base(new PlayerMovementData(direction, speed, isRunning))
        {
        }
    }

    [System.Serializable]
    public class PlayerMovementData
    {
        public Vector3 Direction { get; }
        public float Speed { get; }
        public bool IsRunning { get; }

        public PlayerMovementData(Vector3 direction, float speed, bool isRunning)
        {
            Direction = direction;
            Speed = speed;
            IsRunning = isRunning;
        }
    }

    /// <summary>
    /// Player jump event
    /// </summary>
    public class PlayerJumpEvent : GameEvent<Vector3>
    {
        public Vector3 JumpPosition => Data;

        public PlayerJumpEvent(Vector3 position) : base(position)
        {
        }
    }
}
