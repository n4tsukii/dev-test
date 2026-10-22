using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovements : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private float gravity = -20f;
    private float verticalSpeed;

    private CharacterController controller;
    private Animator anim;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    private void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0, input.y);
        if (direction.magnitude > 1)
            direction.Normalize();

        if (controller.isGrounded && verticalSpeed < 0)
            verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;

        Vector3 move = direction * moveSpeed;
        move.y = verticalSpeed;
        controller.Move(move * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        Vector3 flatVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speed = flatVelocity.magnitude / moveSpeed * 0.6f;
        anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        if (speed < 0.01f && anim.GetFloat("Speed") < 0.01f)
            anim.SetFloat("Speed", 0);
    }
}
