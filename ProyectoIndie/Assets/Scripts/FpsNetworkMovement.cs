
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FpsNetworkMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Renderer[] playerRenderers;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float groundAcceleration = 25f;
    [SerializeField] private float airAcceleration = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -22f;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float cameraPitch;
    private bool cursorLocked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        bool isLocalPlayer = IsOwner;

        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in cameras)
            cam.enabled = isLocalPlayer;

        AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
        foreach (AudioListener listener in listeners)
            listener.enabled = isLocalPlayer;

        if (playerRenderers != null)
        {
            foreach (Renderer playerRenderer in playerRenderers)
            {
                if (playerRenderer != null)
                    playerRenderer.enabled = !isLocalPlayer;
            }
        }

        if (isLocalPlayer)
            LockCursor();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        if (!cursorLocked && Input.GetMouseButtonDown(0))
            LockCursor();

        if (!cursorLocked)
            return;

        Look();
        Move();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
        Vector3 targetVelocity = transform.right * input.x + transform.forward * input.z;

        bool sprinting = Input.GetKey(KeyCode.LeftShift) && z > 0f;
        targetVelocity *= sprinting ? sprintSpeed : walkSpeed;

        float acceleration = controller.isGrounded ? groundAcceleration : airAcceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = jumpForce;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            UnlockCursor();
    }
}
