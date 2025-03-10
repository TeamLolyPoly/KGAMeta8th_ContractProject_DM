using UnityEditor;
using UnityEngine;

public class ArcNotePatternTester : EditorWindow
{
    private CombinedSpawnManager spawnManager;

    [MenuItem("Tools/Arc Note Pattern Tester")]
    private static void Init()
    {
        ArcNotePatternTester window = (ArcNotePatternTester)GetWindow(typeof(ArcNotePatternTester));
        window.Show();
    }

    private void OnGUI()
    {
        spawnManager = FindObjectOfType<CombinedSpawnManager>();

        if (spawnManager == null)
        {
            EditorGUILayout.HelpBox(
                "CombinedSpawnManager를 찾을 수 없습니다!",
                MessageType.Warning
            );
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("단노트 패턴 테스트", EditorStyles.boldLabel);

        if (GUILayout.Button("단노트 패턴 저장"))
        {
            spawnManager.TestSaveGridNotePattern();
        }

        if (GUILayout.Button("단노트 패턴 로드"))
        {
            spawnManager.TestLoadGridNotePattern();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("원형 롱노트 패턴 테스트", EditorStyles.boldLabel);

        if (GUILayout.Button("원형 롱노트 패턴 저장"))
        {
            spawnManager.TestSaveArcNotePattern();
        }

        if (GUILayout.Button("원형 롱노트 패턴 로드"))
        {
            spawnManager.TestLoadArcNotePattern();
        }
    }
}
