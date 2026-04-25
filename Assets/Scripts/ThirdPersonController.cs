using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class ThirdPersonController : MonoBehaviour
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
    [FoldoutGroup("Controller/Dash")]
    public float dashCooldown = 1f;
    [FoldoutGroup("Controller/Dash")]
    private bool canDash = true;
    [FoldoutGroup("Controller/Animator"), SerializeField]
    private CinemachineImpulseSource source;

    [SerializeField] private Vector2 moveInput;



    [FoldoutGroup("WallRun")]
    public float rayLenght = 2f;
    [FoldoutGroup("WallRun")]
    public float cameraTitlt = 15;
    [FoldoutGroup("WallRun")]
    public float maxTimeInAir;
    [FoldoutGroup("WallRun")]
    public bool enableWallRun;

    [FoldoutGroup("WallJump")]
    public float wallJumpForce = 10f;
    [FoldoutGroup("WallJump")]
    public float wallJumpSideForce = 8f;
    [FoldoutGroup("WallJump")]
    public float wallJumpCooldown = 0.5f;
    private bool canWallJump = true;
    private bool isNearWall;
    private Vector3 wallNormal;

    [FoldoutGroup("Damage")]
    public CinemachineImpulseSource damageImpulse;

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
    void Start()
    {

    }
    void Update()
    {
        EnableWallRun();
        OnMove();
        //OnSimpleMove();
    }

    public void OnMove()
    {
        Vector3 cameraForwardDir = characterCamera.transform.forward;
        cameraForwardDir.y = 0;
        cameraForwardDir.Normalize();


        if (moveInput != Vector2.zero)
        {
            Quaternion targetQuaternion = Quaternion.LookRotation(cameraForwardDir);
            //transform.rotation = targetQuaternion;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetQuaternion,
                rotationSpeed * Time.deltaTime);


        }
        //>?
        Vector3 moveDir;
        if (!enableWallRun)
        {
            moveDir = (cameraForwardDir * moveInput.y + transform.right * moveInput.x) * moveSpeed;
        }
        else
        {
            if (moveInput.y != 0)
            {
                moveDir = (crossResult * moveInput.y) * moveSpeed;
            }
            else
            {
                moveDir = crossResult * moveSpeed;
            }



        }

        float magnitud = Mathf.Abs(controller.velocity.magnitude);
        // print(magnitud);
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
            //->convertir el dash a un barrido por el piso! dash con gravedad integrada omaegoto!
            moveDir = transform.forward * dashForce * (dashTimer / dashDuration);

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0)
                IsDashing = false;
        }
        controller.Move(moveDir * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (controller.isGrounded)
        {
            animator.SetTrigger("Jump");
            if (source != null)
                source.GenerateImpulse();
            verticalVelocity = jumpForce;
        }
        else if (isNearWall && canWallJump)
        {
            animator.SetTrigger("Jump");
            if (source != null)
                source.GenerateImpulse();

            verticalVelocity = wallJumpForce;
            Vector3 pushDir = wallNormal * wallJumpSideForce;
            controller.Move(pushDir * Time.deltaTime);

            StartCoroutine(WallJumpCooldown());
        }
    }
    public void OnSimpleMove()
    {
        transform.Rotate(Vector3.up * moveInput.x * rotationSpeed * Time.deltaTime);
        Vector3 moveDir = transform.forward * moveSpeed * moveInput.y;
        controller.SimpleMove(moveDir);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {


        Vector3 pushDir = (hit.transform.position - transform.position).normalized;

        if (hit.rigidbody != null && hit.rigidbody.linearVelocity == Vector3.zero)
        {
            print(hit.gameObject.name);
            hit.rigidbody.AddForce(pushDir * pushForce, ForceMode.Impulse);
        }
    }
    private void OnDash(InputAction.CallbackContext context)
    {
        if (!canDash) return;

        IsDashing = true;
        dashTimer = dashDuration;
        StartCoroutine(DashCooldown());
    }

    private System.Collections.IEnumerator DashCooldown()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private System.Collections.IEnumerator WallJumpCooldown()
    {
        canWallJump = false;
        yield return new WaitForSeconds(wallJumpCooldown);
        canWallJump = true;
    }

    public void EnableWallRun()
    {
        RaycastHit hit = default;
        bool wallDetected = false;

        Debug.DrawRay(transform.position, transform.right * rayLenght, Color.red);
        Debug.DrawRay(transform.position, -transform.right * rayLenght, Color.blue);

        // Detectar pared derecha
        if (Physics.Raycast(transform.position, transform.right, out RaycastHit hitRight, rayLenght))
        {
            if (hitRight.collider.gameObject.tag == "Wall" && !controller.isGrounded)
            {
                hit = hitRight;
                wallDetected = true;
                characterCamera.Lens.Dutch = cameraTitlt;
                Debug.Log("WallRun DERECHA");
            }
        }

        // Detectar pared izquierda (sin else, para que ambos puedan detectarse)
        if (Physics.Raycast(transform.position, -transform.right, out RaycastHit hitLeft, rayLenght))
        {
            if (hitLeft.collider.gameObject.tag == "Wall" && !controller.isGrounded)
            {
                hit = hitLeft;
                wallDetected = true;
                characterCamera.Lens.Dutch = -cameraTitlt;
                Debug.Log("WallRun IZQUIERDA");
            }
        }

        // Activar o desactivar WallRun
        if (wallDetected)
        {
            enableWallRun = true;
            isNearWall = true;
            wallNormal = hit.normal;

            normalDebug = hit.normal;
            impactPoint = hit.point;

            // Calcular dirección de movimiento en la pared
            crossResult = Vector3.Cross(hit.normal, Vector3.up);

            // Asegurar que siempre vaya hacia adelante relativo al jugador
            if (Vector3.Dot(crossResult, transform.forward) < 0)
            {
                crossResult *= -1;
            }

            crossResult.Normalize();
        }
        else
        {
            enableWallRun = false;
            isNearWall = false;
            characterCamera.Lens.Dutch = 0;
        }
    }

    public void TakeDamage()
    {
        if (damageImpulse != null)
            damageImpulse.GenerateImpulse();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawRay(transform.position, transform.right * rayLenght);
        Gizmos.color = Color.navyBlue;
        Gizmos.DrawRay(transform.position, -transform.right * rayLenght);

        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(impactPoint, normalDebug * rayLenght);
        Gizmos.DrawSphere(impactPoint, 0.1f);

        Gizmos.color = Color.orange;
        Gizmos.DrawRay(impactPoint, crossResult * rayLenght);


    }
}