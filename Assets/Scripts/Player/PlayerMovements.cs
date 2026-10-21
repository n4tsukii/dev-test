using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovements : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Animator anim;
    private InputAction movementInput;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (moveAction == null || moveAction.action == null)
        {
            Debug.LogError("Assign Player/Move to Jammo's Move Action.", this);
            enabled = false;
            return;
        }

        // Own this action's lifetime without disabling shared project input.
        movementInput = moveAction.action.Clone();
        movementInput.Enable();
        verticalSpeed = 0f;
    }

    private void OnDisable()
    {
        movementInput?.Disable();
        movementInput?.Dispose();
        movementInput = null;

        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool("IsMoving", false);
    }

    private void Update()
    {
        Vector2 input = Vector2.ClampMagnitude(movementInput.ReadValue<Vector2>(), 1f);
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed += gravity * Time.deltaTime;
        controller.Move((direction * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion facing = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
        }

        if (anim != null)
        {
            Vector3 horizontalVelocity = controller.velocity;
            horizontalVelocity.y = 0f;
            anim.SetBool("IsMoving", horizontalVelocity.sqrMagnitude > 0.01f);
        }
    }
}
