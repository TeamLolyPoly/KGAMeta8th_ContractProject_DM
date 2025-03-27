using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour, IInitializable
{
    public Camera editorCamera;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    public float cameraSpeed = 5f;
    public float zoomSpeed = 2f;
    public float rotationSpeed = 7f;

    private bool isFreeLook = false;

    private float minVerticalAngle = -80f;
    private float maxVerticalAngle = 80f;
    private float rotationX
    {
        get => transform.eulerAngles.x;
        set => transform.eulerAngles = new Vector3(value, transform.eulerAngles.y, 0f);
    }
    private float rotationY
    {
        get => transform.eulerAngles.y;
        set => transform.eulerAngles = new Vector3(transform.eulerAngles.x, value, 0f);
    }

    public void Initialize()
    {
        editorCamera = Camera.main;

        if (editorCamera != null)
        {
            editorCamera.transform.position = new Vector3(0, 5, -5);
            editorCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
        }

        isInitialized = true;
    }

    public void ToggleFreeLook()
    {
        isFreeLook = !isFreeLook;
        if (!isFreeLook)
        {
            StartCoroutine(MoveCameraToFixed());
        }
    }

    public IEnumerator MoveCameraToFixed()
    {
        Vector3 targetPosition = new Vector3(0, 5, editorCamera.transform.position.z);
        Quaternion targetRotation = Quaternion.Euler(30, 0, 0);
        float elapsedTime = 0f;
        float duration = 1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            editorCamera.transform.position = Vector3.Lerp(
                editorCamera.transform.position,
                targetPosition,
                smoothT
            );

            editorCamera.transform.rotation = Quaternion.Lerp(
                editorCamera.transform.rotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        editorCamera.transform.position = targetPosition;
        editorCamera.transform.rotation = targetRotation;
    }

    private void Update()
    {
        if (!isInitialized || editorCamera == null)
            return;

        HandleCameraZoom();

        if (isFreeLook)
            HandleFreeLook();
        else
            HandleCameraMovement();
    }

    private void HandleCameraMovement()
    {
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(0, 0, vertical) * cameraSpeed * Time.deltaTime;
        editorCamera.transform.Translate(movement, Space.World);
    }

    private void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 cameraPosition = editorCamera.transform.position;
            cameraPosition.y -= scroll * zoomSpeed;
            cameraPosition.y = Mathf.Clamp(cameraPosition.y, 2f, 10f);
            editorCamera.transform.position = cameraPosition;
        }
    }

    private void HandleFreeLook()
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
            editorCamera.transform.position += moveDirection * cameraSpeed * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        if (!Keyboard.current.leftShiftKey.isPressed)
        {
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float mouseX = mouseDelta.x * rotationSpeed * 0.01f;
        float mouseY = mouseDelta.y * rotationSpeed * 0.01f * -1;

        rotationY += mouseX;
        rotationX += mouseY;

        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        editorCamera.transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
