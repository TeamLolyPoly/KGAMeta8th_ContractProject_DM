using System.Collections;
using UnityEngine;

namespace NoteEditor
{
    public class EditorManager : Singleton<EditorManager>, IInitializable
    {
        public RailController railController { get; private set; }
        public CellController cellController { get; private set; }
        public NoteEditor editor { get; private set; }
        public NoteEditorPanel editorPanel { get; private set; }

        private Camera editorCamera;

        [SerializeField]
        private float cameraSpeed = 5f;

        [SerializeField]
        private float zoomSpeed = 2f;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            GetResources();
            StartCoroutine(InitializeComponents());
        }

        private IEnumerator InitializeComponents()
        {
            yield return new WaitUntil(() => AudioManager.Instance.IsInitialized);
            yield return new WaitUntil(() => AudioDataManager.Instance.IsInitialized);
            if (railController != null && !railController.IsInitialized)
            {
                railController.Initialize();
            }

            if (cellController != null && !cellController.IsInitialized)
            {
                cellController.Initialize();
            }

            if (editor != null && !editor.IsInitialized)
            {
                editor.Initialize();
            }

            if (editorPanel != null && !editorPanel.IsInitialized)
            {
                editorPanel.Initialize();
            }

            editorCamera = Camera.main;

            if (editorCamera != null)
            {
                editorCamera.transform.position = new Vector3(0, 5, -5);
                editorCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            }

            isInitialized = true;
            Debug.Log("노트 에디터 씬 초기화 완료");
        }

        public void GetResources()
        {
            railController = new GameObject("RailController").AddComponent<RailController>();
            cellController = new GameObject("CellController").AddComponent<CellController>();
            editor = new GameObject("NoteEditor").AddComponent<NoteEditor>();
            editorPanel = Instantiate(
                Resources.Load<NoteEditorPanel>("Prefabs/NoteEditor/UI_Panel_NoteEditor"),
                GameObject.Find("Canvas").transform
            );
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
}
