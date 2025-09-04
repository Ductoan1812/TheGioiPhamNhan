using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool AttackL { get; private set; }
    public bool AttackR { get; private set; }

    void Update()
    {
        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        MoveInput = moveInput.normalized;

        AttackL = Keyboard.current.qKey.wasPressedThisFrame;
        AttackR = Keyboard.current.eKey.wasPressedThisFrame;
    }
}