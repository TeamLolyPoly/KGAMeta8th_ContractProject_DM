using UnityEngine;

public class Segment : Note
{
    public override void Initialize(NoteData data)
    {
        noteData = new NoteData()
        {
            baseType = data.baseType,
            noteType = data.noteType,
            noteAxis = data.noteAxis,
            direction = data.direction,
            startPosition = data.startPosition,
            targetPosition = data.targetPosition,
            noteSpeed = data.noteSpeed,
            isClockwise = data.isClockwise,
            isSymmetric = data.isSymmetric,
            bar = data.bar,
            beat = data.beat,
        };

        isInitialized = true;
        transform.LookAt(noteData.targetPosition);
        spawnDspTime = AudioSettings.dspTime;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;
        float totalDistance = Vector3.Distance(noteData.startPosition, noteData.targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        transform.position = Vector3.Lerp(
            noteData.startPosition,
            noteData.targetPosition,
            progress
        );

        if (progress >= 1f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ScoreSystem.SetScore(0, NoteRatings.Miss);
            }
            Destroy(gameObject);
        }
    }
}
