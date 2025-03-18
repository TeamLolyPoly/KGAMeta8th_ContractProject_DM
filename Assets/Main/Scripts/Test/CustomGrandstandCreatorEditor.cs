using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomGrandstandCreator))]
public class CustomGrandstandCreatorEditor : Editor
{
    private CustomGrandstandCreator creator;
    private bool showPreview = false;
    private Matrix4x4[] previewMatrices;
    private int previewInstances;

    private SerializedProperty geometryProp;
    private SerializedProperty rowsProp;
    private SerializedProperty columnsProp;
    private SerializedProperty horizontalSpacingProp;
    private SerializedProperty verticalSpacingProp;
    private SerializedProperty heightOffsetProp;
    private SerializedProperty spectatorHeightProp;
    private SerializedProperty spectatorForwardProp;
    private SerializedProperty lodDistancesProp;
    private SerializedProperty lodPrefabsProp;
    private SerializedProperty autoGenerateInEditorProp;

    private void OnEnable()
    {
        creator = (CustomGrandstandCreator)target;

        // 프로퍼티 가져오기
        geometryProp = serializedObject.FindProperty("geometry");
        rowsProp = serializedObject.FindProperty("rows");
        columnsProp = serializedObject.FindProperty("columns");
        horizontalSpacingProp = serializedObject.FindProperty("horizontalSpacing");
        verticalSpacingProp = serializedObject.FindProperty("verticalSpacing");
        heightOffsetProp = serializedObject.FindProperty("heightOffset");
        spectatorHeightProp = serializedObject.FindProperty("spectatorHeight");
        spectatorForwardProp = serializedObject.FindProperty("spectatorForward");
        lodDistancesProp = serializedObject.FindProperty("lodDistances");
        lodPrefabsProp = serializedObject.FindProperty("lodPrefabs");
        autoGenerateInEditorProp = serializedObject.FindProperty("autoGenerateInEditor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(geometryProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("그리드 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rowsProp);
        EditorGUILayout.PropertyField(columnsProp);
        EditorGUILayout.PropertyField(horizontalSpacingProp);
        EditorGUILayout.PropertyField(verticalSpacingProp);
        EditorGUILayout.PropertyField(heightOffsetProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("관중 위치 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spectatorHeightProp);
        EditorGUILayout.PropertyField(spectatorForwardProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("에디터 설정", EditorStyles.boldLabel);

        // 자동 생성 옵션
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(
            autoGenerateInEditorProp,
            new GUIContent(
                "자동 관중 생성",
                "인스펙터 값이 변경될 때마다 자동으로 관중을 생성합니다"
            )
        );
        bool autoGenerateChanged = EditorGUI.EndChangeCheck();

        // 자동 생성 옵션이 변경되었고, 비활성화로 바뀌었을 때
        if (autoGenerateChanged && !autoGenerateInEditorProp.boolValue)
        {
            // 현재 관중 제거를 물어봄
            bool clearSpectators = EditorUtility.DisplayDialog(
                "관중 자동 생성 비활성화",
                "자동 생성을 끄면 기존 관중이 남아있게 됩니다. 기존 관중을 제거할까요?",
                "예, 제거합니다",
                "아니오, 그대로 둡니다"
            );

            if (clearSpectators)
            {
                // 기존 관중 제거
                ClearSpectators();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("LOD 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(lodDistancesProp, true);
        EditorGUILayout.PropertyField(lodPrefabsProp, true);

        EditorGUILayout.Space();
        // 나머지 프로퍼티들 표시
        DrawPropertiesExcluding(
            serializedObject,
            new string[]
            {
                "geometry",
                "rows",
                "columns",
                "horizontalSpacing",
                "verticalSpacing",
                "heightOffset",
                "spectatorHeight",
                "spectatorForward",
                "lodDistances",
                "lodPrefabs",
                "autoGenerateInEditor",
                "m_Script",
            }
        );

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        // 미리보기 버튼
        if (
            GUILayout.Button(
                showPreview ? "미리보기 숨기기" : "미리보기 보기",
                GUILayout.Height(30)
            )
        )
        {
            showPreview = !showPreview;
            SceneView.RepaintAll();
        }

        // 자동 생성이 꺼져있을 때만 수동 생성 버튼 표시
        if (!autoGenerateInEditorProp.boolValue)
        {
            // 수동 생성 버튼
            if (GUILayout.Button("관중 수동 생성", GUILayout.Height(30)))
            {
                GenerateSpectators();
            }
        }

        // 관중 제거 버튼
        if (GUILayout.Button("관중 제거", GUILayout.Height(30)))
        {
            ClearSpectators();
        }

        EditorGUILayout.EndHorizontal();

        // 변경 사항이 있으면 미리보기 업데이트
        if (serializedObject.ApplyModifiedProperties())
        {
            CalculatePreviewMatrices();
            SceneView.RepaintAll();
        }

        // 총 관중 수 표시
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            $"총 관중 수: {creator.rows * creator.columns}명\n"
                + (
                    creator.transform.childCount > 0
                        ? $"현재 생성된 관중: {creator.transform.childCount}명"
                        : "현재 생성된 관중 없음"
                ),
            MessageType.Info
        );
    }

    private void OnSceneGUI()
    {
        if (!showPreview || creator == null)
            return;

        if (previewMatrices == null)
        {
            CalculatePreviewMatrices();
        }

        // 리소스 준비
        GameObject previewPrefab = null;
        if (creator.geometry.spectator.Count > 0)
        {
            previewPrefab = creator.geometry.spectator[0];
        }

        if (previewPrefab == null)
            return;

        // 관중 위치 시각화
        Handles.color = new Color(0, 1, 0, 0.2f);

        for (int i = 0; i < previewMatrices.Length; i++)
        {
            Matrix4x4 matrix = previewMatrices[i] * creator.transform.localToWorldMatrix;
            Vector3 position = matrix.MultiplyPoint(Vector3.zero);

            // 위치 표시
            Handles.SphereHandleCap(0, position, Quaternion.identity, 0.1f, EventType.Repaint);

            // 관중 메시 표시 (간략화)
            if (i % 10 == 0) // 10개마다 하나씩만 표시 (성능 최적화)
            {
                Handles.color = new Color(0, 0.8f, 0.5f, 0.5f);
                Handles.CubeHandleCap(
                    0,
                    position,
                    Quaternion.Euler(0, 180, 0),
                    0.5f,
                    EventType.Repaint
                );
                Handles.color = new Color(0, 1, 0, 0.2f);
            }
        }

        // 안내 텍스트
        Handles.BeginGUI();
        GUI.color = Color.white;
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);

        GUILayout.BeginArea(new Rect(10, 10, 200, 50));
        GUILayout.Label($"관중 미리보기: {previewInstances}명", EditorStyles.helpBox);
        GUILayout.EndArea();

        Handles.EndGUI();
    }

    private void CalculatePreviewMatrices()
    {
        if (creator == null)
            return;

        int rows = creator.rows;
        int columns = creator.columns;
        previewInstances = rows * columns;

        previewMatrices = new Matrix4x4[previewInstances];
        int index = 0;

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float xPos = i * creator.horizontalSpacing;
                float yPos = j * creator.heightOffset;
                float zPos = j * creator.verticalSpacing;

                previewMatrices[index++] = Matrix4x4.TRS(
                    new Vector3(
                        xPos,
                        yPos + creator.spectatorHeight,
                        zPos + creator.spectatorForward
                    ),
                    Quaternion.Euler(0, 180, 0),
                    Vector3.one
                );
            }
        }
    }

    private void GenerateSpectators()
    {
        if (creator == null || creator.geometry.spectator.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "관중 프리팹이 설정되지 않았습니다.", "확인");
            return;
        }

        // 확인 대화상자
        if (
            !EditorUtility.DisplayDialog(
                "관중 수동 생성",
                $"관중을 수동으로 생성합니다. 총 {creator.rows * creator.columns}명의 관중이 생성됩니다.\n"
                    + "기존 관중은 모두 제거됩니다. 계속하시겠습니까?",
                "확인",
                "취소"
            )
        )
        {
            return;
        }

        // 기존 자식 오브젝트 삭제
        ClearSpectators();

        // CreateSpectators 메서드 직접 호출
        creator.CreateSpectators();

        EditorUtility.DisplayDialog(
            "완료",
            $"{creator.rows * creator.columns}명의 관중이 생성되었습니다.",
            "확인"
        );
    }

    // 관중 제거 함수
    private void ClearSpectators()
    {
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in creator.transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
        {
            Undo.DestroyObjectImmediate(child);
        }
    }
}
