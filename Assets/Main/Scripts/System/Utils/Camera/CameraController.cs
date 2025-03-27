using UnityEngine;

public class CameraController : MonoBehaviour, IInitializable
{
    private Camera editorCamera;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    public float cameraSpeed = 5f;
    public float zoomSpeed = 2f;

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

    private void Update()
    {
        if (!isInitialized || editorCamera == null)
            return;

        HandleCameraMovement();
        HandleCameraZoom();
    }

    private void HandleCameraMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * cameraSpeed * Time.deltaTime;
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
}
