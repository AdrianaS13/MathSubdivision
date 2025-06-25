using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastSpeed = 20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public KeyCode lookModeKey = KeyCode.LeftShift;  // Hold this key to enable mouse look

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Keep cursor free for point selection
        Cursor.lockState = CursorLockMode.None;

        // Store initial rotation
        Vector3 rot = transform.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        // Only move if ALT is held (so it doesn't interfere with point selection)
        if (Input.GetKey(lookModeKey))
        {
            // Forward/Backward (W/S)
            if (Input.GetKey(KeyCode.W))
                direction += transform.forward;
            if (Input.GetKey(KeyCode.S))
                direction -= transform.forward;

            // Left/Right (A/D)
            if (Input.GetKey(KeyCode.A))
                direction -= transform.right;
            if (Input.GetKey(KeyCode.D))
                direction += transform.right;

            // Up/Down (Space/Ctrl)
            if (Input.GetKey(KeyCode.Space))
                direction += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl))
                direction -= Vector3.up;
        }

        // Choose speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : moveSpeed;

        // Move
        if (direction != Vector3.zero)
        {
            transform.Translate(direction * currentSpeed * Time.deltaTime, Space.World);
        }
    }

    void HandleMouseLook()
    {
        // Only look around when ALT is held
        if (Input.GetKey(lookModeKey))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
    }
}