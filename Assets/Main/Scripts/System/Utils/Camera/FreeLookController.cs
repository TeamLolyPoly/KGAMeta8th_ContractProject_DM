using UnityEngine;
using UnityEngine.InputSystem;

public class FreeLookController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("회전 설정")]
    [SerializeField]
    private float rotationSpeed = 2f;

    [SerializeField]
    private bool invertY = false;

    [SerializeField]
    private float minVerticalAngle = -80f;

    [SerializeField]
    private float maxVerticalAngle = 80f;
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        Vector3 rotation = transform.eulerAngles;
        rotationX = rotation.x;
        rotationY = rotation.y;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed)
            moveDirection += transform.forward;

        if (keyboard.sKey.isPressed)
            moveDirection -= transform.forward;

        if (keyboard.aKey.isPressed)
            moveDirection -= transform.right;

        if (keyboard.dKey.isPressed)
            moveDirection += transform.right;

        if (keyboard.fKey.isPressed)
        {
            if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;
        }

        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        float mouseX = mouseDelta.x * rotationSpeed * 0.01f;
        float mouseY = mouseDelta.y * rotationSpeed * 0.01f * (invertY ? 1 : -1);

        rotationY += mouseX;
        rotationX += mouseY;

        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
