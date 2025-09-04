using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private PlayerInput playerInput;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        Vector2 input = playerInput != null ? playerInput.MoveInput : Vector2.zero;
        bool isMoving = input.magnitude > 0.01f;
        animator.SetBool("1_Move", isMoving);

        if (playerInput.AttackL)
        {
            animator.SetTrigger("2_Attack");
        }
    }
}
