using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.Heat;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarouselMenu : MonoBehaviour
{
    private List<BoxButtonManager> instantiatedItems = new List<BoxButtonManager>();

    [SerializeField]
    private float spacing = 200f;

    [SerializeField]
    private float scaleRatio = 0.7f;

    [SerializeField]
    private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private float moveTime = 0.5f;

    private float[] positions;
    private float currentPosition;
    private int selectedIndex;
    private Coroutine currentMoveCoroutine;

    [SerializeField]
    private BoxButtonManager menuItemPrefab;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private Vector2 itemSize = new Vector2(200, 200);

    private XRPlayer xrPlayer;

    private bool isMoving = false;
    private float joystickCooldown = 0.3f;
    private float lastJoystickTime;

    public void Initialize(Dictionary<string, List<TrackData>> albumDataList)
    {
        InitializeCarousel();
        CreateMenuItems(albumDataList);

        xrPlayer = GameManager.Instance.PlayerSystem.XRPlayer;

        if (xrPlayer != null)
        {
            xrPlayer.LeftController.rotateAnchorAction.action.performed += HandleJoystickMovement;
        }

        selectedIndex = 0;

        if (instantiatedItems.Count > 0)
        {
            currentPosition = 0;
            StartCoroutine(InitialAnimation());
        }
    }

    public void CleanUp()
    {
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }

        if (xrPlayer != null)
        {
            xrPlayer.LeftController.rotateAnchorAction.action.performed -= HandleJoystickMovement;
        }

        foreach (var item in instantiatedItems)
        {
            Destroy(item.gameObject);
        }
        instantiatedItems.Clear();
    }

    private void HandleJoystickMovement(InputAction.CallbackContext context)
    {
        if (Time.time - lastJoystickTime < joystickCooldown || isMoving)
            return;

        if (context.ReadValue<Vector2>().x > 0)
        {
            SelectNext();
        }
        else if (context.ReadValue<Vector2>().x < 0)
        {
            SelectPrevious();
        }

        lastJoystickTime = Time.time;
    }

    private void CreateMenuItems(Dictionary<string, List<TrackData>> albumDataList)
    {
        foreach (var item in instantiatedItems)
        {
            Destroy(item.gameObject);
        }
        instantiatedItems.Clear();
        foreach (var albumData in albumDataList)
        {
            BoxButtonManager item = Instantiate(menuItemPrefab, contentParent);
            item.GetComponent<RectTransform>().sizeDelta = itemSize;
            if (albumData.Value.First().AlbumArt != null)
            {
                item.SetBackground(albumData.Value.First().AlbumArt);
            }
            item.SetText($"{albumData.Key}");
            AddItem(item);
            item.SetInteractable(false);
            item.onClick.AddListener(() => OnItemClick(albumData.Key, albumData.Value));
        }
    }

    private void OnItemClick(string albumName, List<TrackData> albumTracks)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            MultiTrackSelectPanel trackSelectPanel =
                StageUIManager.Instance.OpenPanel(PanelType.Multi_TrackSelect)
                as MultiTrackSelectPanel;
            trackSelectPanel.Initialize(albumTracks);

            StageUIManager.Instance.ClosePanel(PanelType.AlbumSelect);
        }
        else
        {
            SingleTrackSelectPanel trackSelectPanel =
                StageUIManager.Instance.OpenPanel(PanelType.Single_TrackSelect)
                as SingleTrackSelectPanel;
            trackSelectPanel.Initialize(albumTracks);

            StageUIManager.Instance.ClosePanel(PanelType.AlbumSelect);
        }
    }

    private void UpdateItemSelected()
    {
        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            instantiatedItems[i].SetInteractable(isSelected);

            if (isSelected && !Mathf.Approximately(instantiatedItems[i].transform.localScale.x, 1f))
            {
                instantiatedItems[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void InitializeCarousel()
    {
        positions = new float[instantiatedItems.Count];

        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            positions[i] = i * spacing;

            Vector3 targetPosition = instantiatedItems[i].transform.localPosition;
            targetPosition.x = positions[i];
            instantiatedItems[i].transform.localPosition = targetPosition;

            float normalizedDistance = Mathf.Abs(positions[i]) / (spacing * 2);
            float scale = Mathf.Lerp(1f, scaleRatio, scaleCurve.Evaluate(normalizedDistance));
            instantiatedItems[i].transform.localScale = new Vector3(scale, scale, 1f);
        }

        currentPosition = 0;
        selectedIndex = 0;
    }

    private void UpdateCarouselPositionsAndScales(float position)
    {
        float closestDistance = float.MaxValue;
        int newSelectedIndex = selectedIndex;

        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            float distance = Mathf.Abs(positions[i] - position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                newSelectedIndex = i;
            }
        }

        if (newSelectedIndex != selectedIndex)
        {
            selectedIndex = newSelectedIndex;
            UpdateItemSelected();
        }

        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            float distance = positions[i] - position;

            Vector3 targetPosition = instantiatedItems[i].transform.localPosition;
            targetPosition.x = distance;
            instantiatedItems[i].transform.localPosition = targetPosition;

            float normalizedDistance = Mathf.Abs(distance) / (spacing * 2);
            float scale = Mathf.Lerp(1f, scaleRatio, scaleCurve.Evaluate(normalizedDistance));
            instantiatedItems[i].transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private IEnumerator MoveCarousel(float targetPos)
    {
        float startPos = currentPosition;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveTime;

            t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            currentPosition = Mathf.Lerp(startPos, targetPos, t);
            UpdateCarouselPositionsAndScales(currentPosition);

            yield return null;
        }

        currentPosition = targetPos;
        UpdateCarouselPositionsAndScales(currentPosition);
    }

    public void SelectNext()
    {
        if (selectedIndex < instantiatedItems.Count - 1)
        {
            selectedIndex++;
            float newTargetPosition = positions[selectedIndex];

            if (currentMoveCoroutine != null)
            {
                StopCoroutine(currentMoveCoroutine);
            }

            currentMoveCoroutine = StartCoroutine(MoveCarousel(newTargetPosition));
        }
    }

    public void SelectPrevious()
    {
        if (selectedIndex > 0)
        {
            selectedIndex--;
            float newTargetPosition = positions[selectedIndex];

            if (currentMoveCoroutine != null)
            {
                StopCoroutine(currentMoveCoroutine);
            }

            currentMoveCoroutine = StartCoroutine(MoveCarousel(newTargetPosition));
        }
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public void AddItem(BoxButtonManager item)
    {
        instantiatedItems.Add(item);
        InitializeCarousel();
    }

    public void RemoveItem(BoxButtonManager item)
    {
        if (instantiatedItems.Contains(item))
        {
            instantiatedItems.Remove(item);
            InitializeCarousel();
        }
    }

    private IEnumerator InitialAnimation()
    {
        float contentWidth = contentParent.GetComponent<RectTransform>().rect.width;
        float contentHeight = contentParent.GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            int spawnArea = Random.Range(0, 4);
            float randomX,
                randomY;

            switch (spawnArea)
            {
                case 0:
                    randomX = Random.Range(-contentWidth * 1.5f, -contentWidth);
                    randomY = Random.Range(-contentHeight, contentHeight);
                    break;
                case 1:
                    randomX = Random.Range(contentWidth, contentWidth * 1.5f);
                    randomY = Random.Range(-contentHeight, contentHeight);
                    break;
                case 2:
                    randomX = Random.Range(-contentWidth, contentWidth);
                    randomY = Random.Range(contentHeight, contentHeight * 1.5f);
                    break;
                default:
                    randomX = Random.Range(-contentWidth, contentWidth);
                    randomY = Random.Range(-contentHeight * 1.5f, -contentHeight);
                    break;
            }

            instantiatedItems[i].transform.localPosition = new Vector3(randomX, randomY, 0f);
            instantiatedItems[i].transform.localScale = Vector3.zero;
        }

        float gatherTime = 1.2f;
        float elapsedTime = 0f;

        Vector3[] startPositions = new Vector3[instantiatedItems.Count];
        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            startPositions[i] = instantiatedItems[i].transform.localPosition;
        }

        while (elapsedTime < gatherTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / gatherTime;

            t = 1f - Mathf.Pow(1f - t, 4f);

            for (int i = 0; i < instantiatedItems.Count; i++)
            {
                instantiatedItems[i].transform.localPosition = Vector3.Lerp(
                    startPositions[i],
                    Vector3.zero,
                    t
                );
                instantiatedItems[i].transform.localScale = Vector3.Lerp(
                    Vector3.zero,
                    Vector3.one * 0.8f,
                    t
                );
            }

            yield return null;
        }

        float spreadTime = 1f;
        elapsedTime = 0f;

        Vector3[] targetPositions = new Vector3[instantiatedItems.Count];
        Vector3[] targetScales = new Vector3[instantiatedItems.Count];

        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            float distance = positions[i] - currentPosition;
            targetPositions[i] = new Vector3(distance, 0f, 0f);

            float normalizedDistance = Mathf.Abs(distance) / (spacing * 2);
            float scale = Mathf.Lerp(1f, scaleRatio, scaleCurve.Evaluate(normalizedDistance));
            targetScales[i] = new Vector3(scale, scale, 1f);
        }
        targetScales[selectedIndex] = Vector3.one;

        while (elapsedTime < spreadTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / spreadTime;

            t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            for (int i = 0; i < instantiatedItems.Count; i++)
            {
                instantiatedItems[i].transform.localPosition = Vector3.Lerp(
                    Vector3.zero,
                    targetPositions[i],
                    t
                );
                instantiatedItems[i].transform.localScale = Vector3.Lerp(
                    Vector3.one * 0.8f,
                    targetScales[i],
                    t
                );
            }

            yield return null;
        }

        UpdateCarouselPositionsAndScales(currentPosition);
        UpdateItemSelected();
    }
}
