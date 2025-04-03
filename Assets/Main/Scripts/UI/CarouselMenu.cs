using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;

public class CarouselMenu : MonoBehaviour
{
    [SerializeField]
    private List<RectTransform> menuItems = new List<RectTransform>();

    [SerializeField]
    private ButtonManager leftButton;

    [SerializeField]
    private ButtonManager rightButton;

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
    private int itemCount = 5;

    [SerializeField]
    private Vector2 itemSize = new Vector2(200, 200);

    private List<BoxButtonManager> instantiatedItems = new List<BoxButtonManager>();

    void Start()
    {
        InitializeCarousel();
        CreateMenuItems();
        SetupButtons();

        selectedIndex = 0;

        if (menuItems.Count > 0)
        {
            currentPosition = 0;
            StartCoroutine(InitialAnimation());
        }
    }

    private void SetupButtons()
    {
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(SelectPrevious);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(SelectNext);
        }
    }

    private void CreateMenuItems()
    {
        foreach (var item in instantiatedItems)
        {
            Destroy(item.gameObject);
        }
        instantiatedItems.Clear();

        for (int i = 0; i < itemCount; i++)
        {
            BoxButtonManager item = Instantiate(menuItemPrefab, contentParent);
            RectTransform rectTransform = item.GetComponent<RectTransform>();
            rectTransform.sizeDelta = itemSize;
            AddItem(rectTransform);
            instantiatedItems.Add(item);
            item.SetInteractable(false);
        }
    }

    private void UpdateItemSelected()
    {
        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            instantiatedItems[i].SetInteractable(isSelected);

            if (isSelected && !Mathf.Approximately(menuItems[i].localScale.x, 1f))
            {
                menuItems[i].localScale = Vector3.one;
            }
        }
    }

    private void InitializeCarousel()
    {
        positions = new float[menuItems.Count];

        for (int i = 0; i < menuItems.Count; i++)
        {
            positions[i] = i * spacing;

            Vector3 targetPosition = menuItems[i].localPosition;
            targetPosition.x = positions[i];
            menuItems[i].localPosition = targetPosition;

            float normalizedDistance = Mathf.Abs(positions[i]) / (spacing * 2);
            float scale = Mathf.Lerp(1f, scaleRatio, scaleCurve.Evaluate(normalizedDistance));
            menuItems[i].localScale = new Vector3(scale, scale, 1f);
        }

        currentPosition = 0;
        selectedIndex = 0;
    }

    private void UpdateCarouselPositionsAndScales(float position)
    {
        float closestDistance = float.MaxValue;
        int newSelectedIndex = selectedIndex;

        for (int i = 0; i < menuItems.Count; i++)
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

        for (int i = 0; i < menuItems.Count; i++)
        {
            float distance = positions[i] - position;

            Vector3 targetPosition = menuItems[i].localPosition;
            targetPosition.x = distance;
            menuItems[i].localPosition = targetPosition;

            float normalizedDistance = Mathf.Abs(distance) / (spacing * 2);
            float scale = Mathf.Lerp(1f, scaleRatio, scaleCurve.Evaluate(normalizedDistance));
            menuItems[i].localScale = new Vector3(scale, scale, 1f);
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
        if (selectedIndex < menuItems.Count - 1)
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

    public void AddItem(RectTransform item)
    {
        menuItems.Add(item);
        InitializeCarousel();
    }

    public void RemoveItem(RectTransform item)
    {
        if (menuItems.Contains(item))
        {
            menuItems.Remove(item);
            InitializeCarousel();
        }
    }

    public void ReloadItems(int newItemCount)
    {
        itemCount = newItemCount;
        CreateMenuItems();
    }

    private void OnDestroy()
    {
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }

        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(SelectPrevious);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(SelectNext);
        }
    }

    private IEnumerator InitialAnimation()
    {
        float contentWidth = contentParent.GetComponent<RectTransform>().rect.width;
        float contentHeight = contentParent.GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < menuItems.Count; i++)
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

            menuItems[i].localPosition = new Vector3(randomX, randomY, 0f);
            menuItems[i].localScale = Vector3.zero;
        }

        float gatherTime = 1.2f;
        float elapsedTime = 0f;

        Vector3[] startPositions = new Vector3[menuItems.Count];
        for (int i = 0; i < menuItems.Count; i++)
        {
            startPositions[i] = menuItems[i].localPosition;
        }

        while (elapsedTime < gatherTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / gatherTime;

            t = 1f - Mathf.Pow(1f - t, 4f);

            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].localPosition = Vector3.Lerp(startPositions[i], Vector3.zero, t);
                menuItems[i].localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 0.8f, t);
            }

            yield return null;
        }

        float spreadTime = 1f;
        elapsedTime = 0f;

        Vector3[] targetPositions = new Vector3[menuItems.Count];
        Vector3[] targetScales = new Vector3[menuItems.Count];

        for (int i = 0; i < menuItems.Count; i++)
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

            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].localPosition = Vector3.Lerp(Vector3.zero, targetPositions[i], t);
                menuItems[i].localScale = Vector3.Lerp(Vector3.one * 0.8f, targetScales[i], t);
            }

            yield return null;
        }

        UpdateCarouselPositionsAndScales(currentPosition);
        UpdateItemSelected();
    }
}
