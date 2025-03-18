using UnityEngine;

namespace NoteEditor
{
    public class LongNoteModel : MonoBehaviour
    {
        [SerializeField]
        private GameObject rod;

        [SerializeField]
        private GameObject symmetricRod;

        [SerializeField]
        private GameObject startPoint;

        [SerializeField]
        private GameObject endPoint;

        [SerializeField]
        private GameObject symmetricEndPoint;

        [SerializeField]
        private Transform centerPivot;

        private Cell startCell;
        private Cell endCell;
        private NoteData noteData;
        private bool isSymmetric = false;
        private Vector3 gridCenterPosition;

        public void Initialize(Cell start, Cell end, NoteData data)
        {
            startCell = start;
            endCell = end;
            noteData = data;
            isSymmetric = data.isSymmetric;
            gridCenterPosition = CalculateGridCenter();

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
                    1,
                    1
                );

                if (centralCell != null)
                {
                    return centralCell.transform.position;
                }
            }

            return startCell.transform.position;
        }

        public void UpdateVisual()
        {
            if (startCell == null || endCell == null)
                return;

            Vector3 startPos = startCell.transform.position;
            Vector3 endPos = endCell.transform.position;

            if (!isSymmetric)
            {
                transform.position = (startPos + endPos) / 2f;

                float distance = Vector3.Distance(startPos, endPos);
                rod.transform.localScale = new Vector3(0.1f, distance / 2f, 0.1f);
                rod.transform.LookAt(endPos);
                rod.transform.Rotate(90, 0, 0);

                startPoint.transform.position = startPos;
                endPoint.transform.position = endPos;

                if (symmetricRod != null)
                    symmetricRod.SetActive(false);
                if (symmetricEndPoint != null)
                    symmetricEndPoint.SetActive(false);

                rod.SetActive(true);
                startPoint.SetActive(true);
                endPoint.SetActive(true);
            }
            else
            {
                transform.position = gridCenterPosition;
                if (centerPivot != null)
                    centerPivot.position = gridCenterPosition;

                float distance1 = Vector3.Distance(startPos, gridCenterPosition);
                rod.transform.localScale = new Vector3(0.1f, distance1 / 2f, 0.1f);
                rod.transform.position = (startPos + gridCenterPosition) / 2f;
                rod.transform.LookAt(gridCenterPosition);
                rod.transform.Rotate(90, 0, 0);

                Vector3 direction = (gridCenterPosition - startPos).normalized;
                Vector3 symmetricPos = gridCenterPosition + direction * distance1;

                if (symmetricRod != null)
                {
                    symmetricRod.transform.localScale = new Vector3(0.1f, distance1 / 2f, 0.1f);
                    symmetricRod.transform.position = (gridCenterPosition + symmetricPos) / 2f;
                    symmetricRod.transform.LookAt(symmetricPos);
                    symmetricRod.transform.Rotate(90, 0, 0);
                    symmetricRod.SetActive(true);
                }

                startPoint.transform.position = startPos;

                if (symmetricEndPoint != null)
                {
                    symmetricEndPoint.transform.position = symmetricPos;
                    symmetricEndPoint.SetActive(true);
                }

                if (endPoint != null)
                    endPoint.SetActive(false);

                rod.SetActive(true);
                startPoint.SetActive(true);
            }
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
            }
        }
    }
}
