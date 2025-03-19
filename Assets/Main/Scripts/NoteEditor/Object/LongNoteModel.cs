using UnityEngine;

namespace NoteEditor
{
    public class LongNoteModel : MonoBehaviour
    {
        [SerializeField]
        private GameObject rod;

        [SerializeField]
        private GameObject startPoint;

        [SerializeField]
        private GameObject endPoint;

        private Cell startCell;
        private Cell endCell;
        private NoteData noteData;
        private bool isInitialized = false;

        private void Awake()
        {
            if (rod == null || startPoint == null || endPoint == null)
            {
                Debug.LogError(
                    "[LongNoteModel] 롱노트 모델에 필요한 컴포넌트가 할당되지 않았습니다."
                );
            }

            if (rod != null)
                rod.SetActive(true);
            if (startPoint != null)
                startPoint.SetActive(true);
            if (endPoint != null)
                endPoint.SetActive(true);
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
            isInitialized = true;

            ApplyRodColors();
            UpdateVisual();

            Debug.Log(
                $"롱노트 모델 초기화: 시작 인덱스={data.startIndex}, 끝 인덱스={data.endIndex}, 대칭={data.isSymmetric}, 시계방향={data.isClockwise}"
            );
        }

        public void UpdateVisual()
        {
            if (startCell == null || endCell == null)
                return;

            Vector3 startPos = startCell.transform.position;
            Vector3 endPos = endCell.transform.position;

            if (rod == null || startPoint == null || endPoint == null)
            {
                Debug.LogError("[LongNoteModel] 롱노트 시각화에 필요한 컴포넌트가 없습니다.");
                return;
            }

            // 롱노트 모델을 시작점과 끝점 사이의 중앙에 배치
            transform.position = (startPos + endPos) / 2f;

            // 시작점과 끝점 사이의 거리 계산
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

            startPoint.transform.position = startPos;
            endPoint.transform.position = endPos;
        }

        public void ApplyRodColors()
        {
            Color startColor = Color.red;
            Color endColor = Color.blue;
            Color rodColor = Color.gray;

            ApplyColorToRenderer(startPoint, startColor);
            ApplyColorToRenderer(endPoint, endColor);
            ApplyColorToRenderer(rod, rodColor);
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

        public void UpdateEndPoint(Cell newEndCell)
        {
            endCell = newEndCell;
            UpdateVisual();
        }

        public void SetSymmetric(bool isSymmetric)
        {
            if (noteData != null)
            {
                noteData.isSymmetric = isSymmetric;
                Debug.Log($"롱노트 대칭 설정: {isSymmetric}");
            }
        }

        public void SetClockwise(bool isClockwise)
        {
            if (noteData != null)
            {
                noteData.isClockwise = isClockwise;
                Debug.Log($"롱노트 회전 방향 설정: {isClockwise}");
            }
        }
    }
}
