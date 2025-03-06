using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeCameraController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;
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

         // 새로운 Input System 사용
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.wKey.isPressed)
            moveDirection += transform.forward;

        if (keyboard.sKey.isPressed)
            moveDirection -= transform.forward;

        if (keyboard.aKey.isPressed)
            moveDirection -= transform.right;

        if (keyboard.dKey.isPressed)
            moveDirection += transform.right;

        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        // 새로운 Input System 사용
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 마우스 델타 값 가져오기
        Vector2 mouseDelta = mouse.delta.ReadValue();
        float mouseX = mouseDelta.x * rotationSpeed * 0.01f; // 감도 조정
        float mouseY = mouseDelta.y * rotationSpeed * 0.01f * (invertY ? 1 : -1);

        rotationY += mouseX;
        rotationX += mouseY;

        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

}
