using UnityEngine;

public class LongNote : Note
{
    public void SetPositions(Vector3 start, Vector3 target)
    {
        startPosition = start;
        targetPosition = target;
        transform.position = start;
        transform.rotation = Quaternion.Euler(90f, 0, 0);
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        if (progress >= 1f)
        {
            scoreSystem.SetScore(0, NoteRatings.Miss);
            Destroy(gameObject);
        }
    }
}
