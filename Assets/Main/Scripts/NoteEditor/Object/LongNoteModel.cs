using UnityEngine;

namespace NoteEditor
{
    public class LongNoteModel : MonoBehaviour
    {
        [SerializeField]
        private GameObject rod;

        [SerializeField]
        private GameObject redPoint;

        [SerializeField]
        private GameObject bluePoint;

        private GameObject symmetricRod;
        private GameObject symmetricBluePoint;
        private Transform centerPivot;

        private Cell startCell;
        private Cell endCell;
        private NoteData noteData;
        private bool isSymmetric = false;
        private Vector3 gridCenterPosition;
        private bool isInitialized = false;

        private void Awake()
        {
            if (rod == null || redPoint == null || bluePoint == null)
            {
                Debug.LogError(
                    "[LongNoteModel] 롱노트 모델에 필요한 컴포넌트가 할당되지 않았습니다."
                );
            }

            if (rod != null)
                rod.SetActive(true);
            if (redPoint != null)
                redPoint.SetActive(true);
            if (bluePoint != null)
                bluePoint.SetActive(true);
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                UpdateVisual();
            }
        }

        public void Initialize(Cell start, Cell end, NoteData data)
        {
            if (start == null || end == null || data == null)
            {
                Debug.LogError("[LongNoteModel] 롱노트 초기화에 필요한 데이터가 누락되었습니다.");
                return;
            }

            startCell = start;
            endCell = end;
            noteData = data;
            isSymmetric = data.isSymmetric;
            gridCenterPosition = CalculateGridCenter();
            isInitialized = true;

            ApplyRodColors();

            SetSymmetric(isSymmetric);
            UpdateVisual();
        }

        private Vector3 CalculateGridCenter()
        {
            var editorManager = EditorManager.Instance;
            if (editorManager != null && editorManager.cellController != null)
            {
                Cell centralCell = editorManager.cellController.GetCell(
                    startCell.bar,
                    startCell.beat,
                    3,
                    1
                );

                if (centralCell != null)
                {
                    return centralCell.transform.position;
                }
                else
                {
                    Debug.LogWarning(
                        $"[LongNoteModel] 중앙 셀(bar: {startCell.bar}, beat: {startCell.beat}, x: 3, y: 3)을 찾을 수 없습니다."
                    );
                }
            }

            Debug.LogWarning(
                "[LongNoteModel] 중앙 셀을 찾을 수 없어 시작 셀의 위치를 대신 사용합니다."
            );
            return startCell.transform.position;
        }

        public void UpdateVisual()
        {
            if (startCell == null || endCell == null)
                return;

            Vector3 startPos = startCell.transform.position;
            Vector3 endPos = endCell.transform.position;

            if (rod == null || redPoint == null)
            {
                Debug.LogError("[LongNoteModel] 롱노트 시각화에 필요한 컴포넌트가 없습니다.");
                return;
            }

            if (!isSymmetric)
            {
                transform.position = (startPos + endPos) / 2f;

                float distance = Vector3.Distance(startPos, endPos);
                if (distance <= 0.001f)
                {
                    Debug.LogWarning("[LongNoteModel] 시작점과 끝점이 너무 가깝습니다.");
                    distance = 0.1f;
                }

                rod.transform.localScale = new Vector3(0.1f, distance / 2f, 0.1f);

                rod.transform.position = (startPos + endPos) / 2f;

                Vector3 direction = (endPos - startPos).normalized;
                if (direction.magnitude > 0.001f)
                {
                    rod.transform.rotation =
                        Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
                }

                redPoint.transform.position = startPos;
                bluePoint.transform.position = endPos;

                DestroySymmetricComponents();

                rod.SetActive(true);
                redPoint.SetActive(true);
                bluePoint.SetActive(true);
            }
            else
            {
                CreateSymmetricComponentsIfNeeded();

                transform.position = gridCenterPosition;
                if (centerPivot != null)
                    centerPivot.position = gridCenterPosition;

                float distance1 = Vector3.Distance(startPos, gridCenterPosition);
                if (distance1 <= 0.001f)
                {
                    Debug.LogWarning("[LongNoteModel] 시작점과 중심점이 너무 가깝습니다.");
                    distance1 = 0.1f;
                }

                rod.transform.localScale = new Vector3(0.1f, distance1 / 2f, 0.1f);
                rod.transform.position = (startPos + gridCenterPosition) / 2f;

                Vector3 direction = (gridCenterPosition - startPos).normalized;
                if (direction.magnitude > 0.001f)
                {
                    rod.transform.rotation =
                        Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
                }

                Vector3 symmetricPos;

                if (noteData != null && noteData.isClockwise)
                {
                    Vector3 rotatedDirection = Quaternion.Euler(0, 90, 0) * direction;
                    symmetricPos = gridCenterPosition + rotatedDirection * distance1;
                }
                else
                {
                    Vector3 rotatedDirection = Quaternion.Euler(0, -90, 0) * direction;
                    symmetricPos = gridCenterPosition + rotatedDirection * distance1;
                }

                if (symmetricRod != null)
                {
                    symmetricRod.transform.localScale = new Vector3(0.1f, distance1 / 2f, 0.1f);
                    symmetricRod.transform.position = (gridCenterPosition + symmetricPos) / 2f;

                    Vector3 symDirection = (symmetricPos - gridCenterPosition).normalized;
                    if (symDirection.magnitude > 0.001f)
                    {
                        symmetricRod.transform.rotation =
                            Quaternion.LookRotation(symDirection) * Quaternion.Euler(90, 0, 0);
                    }

                    symmetricRod.SetActive(true);
                }

                redPoint.transform.position = startPos;

                if (symmetricBluePoint != null)
                {
                    symmetricBluePoint.transform.position = symmetricPos;
                    symmetricBluePoint.SetActive(true);
                }

                if (bluePoint != null)
                    bluePoint.SetActive(false);

                rod.SetActive(true);
                redPoint.SetActive(true);
            }

            ApplyRodColors();
        }

        private void CreateSymmetricComponentsIfNeeded()
        {
            if (symmetricRod == null)
            {
                symmetricRod = Instantiate(rod, transform);
                symmetricRod.name = "SymmetricRod";
            }

            if (symmetricBluePoint == null)
            {
                symmetricBluePoint = Instantiate(bluePoint, transform);
                symmetricBluePoint.name = "SymmetricBluePoint";
            }

            if (centerPivot == null)
            {
                GameObject pivotObj = new GameObject("CenterPivot");
                centerPivot = pivotObj.transform;
                centerPivot.SetParent(transform);
            }
        }

        private void DestroySymmetricComponents()
        {
            if (symmetricRod != null)
            {
                symmetricRod.SetActive(false);
            }

            if (symmetricBluePoint != null)
            {
                symmetricBluePoint.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            DestroySymmetricComponents();
        }

        public void UpdateEndPoint(Cell newEndCell)
        {
            endCell = newEndCell;
            UpdateVisual();
        }

        public void SetSymmetric(bool isSymmetric)
        {
            this.isSymmetric = isSymmetric;
            UpdateVisual();
        }

        public void SetClockwise(bool isClockwise)
        {
            if (noteData != null)
            {
                noteData.isClockwise = isClockwise;

                if (isSymmetric)
                {
                    UpdateVisual();
                }
            }
        }

        public void ApplyRodColors()
        {
            ApplyColorToRenderer(redPoint, Color.red);

            if (bluePoint != null)
                ApplyColorToRenderer(bluePoint, Color.blue);

            if (symmetricBluePoint != null)
                ApplyColorToRenderer(symmetricBluePoint, Color.blue);

            Color rodColor = new Color(0.7f, 0.7f, 0.7f);
            ApplyColorToRenderer(rod, rodColor);

            if (symmetricRod != null)
                ApplyColorToRenderer(symmetricRod, rodColor);
        }

        private void ApplyColorToRenderer(GameObject targetObject, Color color)
        {
            if (targetObject == null)
                return;

            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
