using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using System.Collections;

public class ThirdPersonController6 : MonoBehaviour
{
    [FoldoutGroup("References")]
    public InputSystem_Actions inputs;

    [FoldoutGroup("References")]
    private CharacterController controller;

    [FoldoutGroup("References")]
    public CinemachineCamera characterCamera;

    [FoldoutGroup("References")]
    public Animator animator;

    [FoldoutGroup("Controller")]
    public float moveSpeed = 5f;

    [FoldoutGroup("Controller")]
    public float rotationSpeed = 200f;

    [FoldoutGroup("Controller")]
    public float verticalVelocity = 0;

    [FoldoutGroup("Controller")]
    public float jumpForce = 10;

    [FoldoutGroup("Controller")]
    public float pushForce = 4;

    [FoldoutGroup("Controller/Dash")]
    private bool IsDashing;

    [FoldoutGroup("Controller/Dash")]
    public float dashForce;

    [FoldoutGroup("Controller/Dash")]
    public float dashDuration = 0.2f;

    [FoldoutGroup("Controller/Dash")]
    private float dashTimer;

    [FoldoutGroup("Controller/Animator"), SerializeField]
    private CinemachineImpulseSource source;

    [SerializeField]
    private Vector2 moveInput;

    [FoldoutGroup("WallRun")]
    public float rayLenght;

    [FoldoutGroup("WallRun")]
    public float cameraTitlt = 15;

    [FoldoutGroup("WallRun")]
    public float maxTimeInAir;

    [FoldoutGroup("WallRun")]
    public bool enableWallRun;

    [FoldoutGroup("Controller/Dash")]
    public float dashCooldown = 1f;

    private bool canDash = true;

    [FoldoutGroup("WallJump")]
    public float wallJumpForce = 10f;

    [FoldoutGroup("WallJump")]
    public float wallJumpUpForce = 8f;

    [FoldoutGroup("WallJump")]
    public float wallJumpCooldown = 1f;

    private bool canWallJump = true;

    Vector3 normalDebug;
    Vector3 impactPoint;
    Vector3 crossResult;

    private void Awake()
    {
        inputs = new();
        controller = GetComponent<CharacterController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        inputs.Enable();

        inputs.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputs.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputs.Player.Jump.performed += OnJump;
        inputs.Player.Sprint.performed += OnDash;
    }

    void Update()
    {
        EnableWallRun();
        OnMove();
    }

    public void TakeDamage()
    {
        source.GenerateImpulse(0.7f);
    }

    public void OnMove()
    {
        Vector3 cameraForwardDir = characterCamera.transform.forward;
        cameraForwardDir.y = 0;
        cameraForwardDir.Normalize();

        if (moveInput != Vector2.zero)
        {
            Quaternion targetQuaternion = Quaternion.LookRotation(cameraForwardDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetQuaternion,
                rotationSpeed * Time.deltaTime
            );
        }

        Vector3 moveDir;

        if (!enableWallRun)
            moveDir = (cameraForwardDir * moveInput.y + transform.right * moveInput.x) * moveSpeed;
        else
            moveDir = (crossResult * moveInput.y) * moveSpeed;

        float magnitud = Mathf.Abs(controller.velocity.magnitude);
        animator.SetFloat("Speed", magnitud);

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        if (enableWallRun)
            verticalVelocity = 0;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        moveDir.y = verticalVelocity;

        animator.SetBool("Grounded", controller.isGrounded);

        if (IsDashing)
        {
            moveDir = transform.forward * dashForce * (dashTimer / dashDuration);

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
                IsDashing = false;
        }

        controller.Move(moveDir * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (enableWallRun && canWallJump && !controller.isGrounded)
        {
            Vector3 jumpDir = normalDebug + Vector3.up;

            verticalVelocity = wallJumpUpForce;

            controller.Move(jumpDir.normalized * wallJumpForce * Time.deltaTime);

            source.GenerateImpulse(0.4f);
            enableWallRun = false; 

            StartCoroutine(WallJumpCooldown());
            return;
        }

        if (!controller.isGrounded) return;

        animator.SetTrigger("Jump");
        source.GenerateImpulse();
        verticalVelocity = jumpForce;
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!canDash || IsDashing) return;

        StartCoroutine(DashCoroutine());
    }

    public void EnableWallRun()
    {
        if (!canWallJump) // hace que no puedas volver a hacer wallrun inmediatamente después de un walljump
        {
            enableWallRun = false;
            characterCamera.Lens.Dutch = 0; 
            return;
        }
    

        RaycastHit hit = default;

        Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight, rayLenght);
        Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, rayLenght);

        if (hitRight.collider != null && hitRight.collider.gameObject.tag == "Wall")
        {
            hit = hitRight;
            characterCamera.Lens.Dutch = cameraTitlt;
        }
        else if (hitLeft.collider != null && hitLeft.collider.gameObject.tag == "Wall")
        {
            hit = hitLeft;
            characterCamera.Lens.Dutch = -cameraTitlt;
        }
        else
        {
            characterCamera.Lens.Dutch = 0;
            enableWallRun = false;
        }

        if (hit.collider != null)
        {
            enableWallRun = true;

            normalDebug = hit.normal;
            impactPoint = hit.point;
            crossResult = Vector3.Cross(normalDebug, transform.up);

            if (Vector3.Dot(crossResult, transform.forward) < 0)
                crossResult *= -1;
        }
    }

    IEnumerator WallJumpCooldown()
    {
        canWallJump = false;
        yield return new WaitForSeconds(wallJumpCooldown);
        canWallJump = true;
    }

    IEnumerator DashCoroutine()
    {
        canDash = false;
        IsDashing = true;
        dashTimer = dashDuration;

        yield return new WaitForSeconds(dashDuration);

        IsDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}