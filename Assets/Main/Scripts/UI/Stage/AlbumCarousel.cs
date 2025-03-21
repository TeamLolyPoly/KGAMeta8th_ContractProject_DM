using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRCarousel : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField]
    private List<RectTransform> albums;

    [SerializeField]
    private float albumSpacing;

    [SerializeField]
    private Button leftButton;

    [SerializeField]
    private Button rightButton;

    private void Start()
    {
        leftButton.onClick.AddListener(OnLeftButtonClick);
        rightButton.onClick.AddListener(OnRightButtonClick);
    }

    private void OnLeftButtonClick()
    {
        Debug.Log("Left button clicked");
    }

    private void OnRightButtonClick()
    {
        Debug.Log("Right button clicked");
    }
}
