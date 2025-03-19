using UnityEngine;
using UnityEngine.InputSystem;

namespace NoteEditor
{
    public class Cell : MonoBehaviour
    {
        public NoteData noteData;
        public Vector2 cellPosition;
        public int bar;
        public int beat;
        public ShortNoteModel noteModel;
        public LongNoteModel longNoteModel;
        public GameObject cellRenderer;

        private CellController cellController;
        private Camera mainCamera;
        private Collider cellCollider;

        private void Awake()
        {
            cellController = GetComponentInParent<CellController>();
            mainCamera = Camera.main;

            cellCollider = GetComponent<SphereCollider>();
            if (cellCollider == null)
            {
                Debug.LogWarning("셀의 자식 오브젝트에서 SphereCollider를 찾을 수 없습니다.");
            }
        }

        public void Initialize(int bar, int beat, Vector2 cellPosition)
        {
            this.bar = bar;
            this.beat = beat;
            this.cellPosition = cellPosition;
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == cellCollider)
                    {
                        if (cellController != null)
                        {
                            cellController.SelectCell(this);
                        }
                    }
                }
            }
        }
    }
}
