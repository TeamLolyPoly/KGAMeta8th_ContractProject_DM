using UnityEngine;

public class Note : MonoBehaviour, IInitializable
{
    protected int noteScore = 0;
    protected NoteData noteData;
    protected double spawnDspTime;
    protected GameObject hitFX;
    protected ScoreSystem scoreSystem;
    protected Vector3 startPosition;
    protected Vector3 targetPosition;
    public GameObject Rim;

    protected bool isInitialized = false;
    protected bool isHit = false;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        isInitialized = true;
    }

    public void SetNoteColor(Color color)
    {
        Rim.GetComponent<Renderer>().material.color = color;
    }

    public virtual void Initialize(NoteData data)
    {
        noteData = data;

        startPosition = noteData.GetStartPosition();
        targetPosition = noteData.GetTargetPosition();

        transform.position = startPosition;
        transform.LookAt(targetPosition);

        scoreSystem = GameManager.Instance.ScoreSystem;
        isInitialized = true;
        spawnDspTime = AudioSettings.dspTime;
    }

    public static Note CreateNote(NoteData data, Note shortNotePrefab, Note longNotePrefab)
    {
        Note notePrefab;

        switch (data.baseType)
        {
            case NoteBaseType.Short:
                notePrefab = shortNotePrefab;
                break;
            case NoteBaseType.Long:
                notePrefab = longNotePrefab;
                break;
            default:
                Debug.LogError($"Unsupported note type: {data.baseType}");
                return null;
        }

        Vector3 spawnPosition = data.GetStartPosition();
        Note noteInstance = Instantiate(notePrefab, spawnPosition, Quaternion.identity);
        noteInstance.Initialize(data);

        return noteInstance;
    }

    protected virtual void OnCollisionEnter(Collision other)
    {
        if (!isInitialized)
            return;
        if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
        {
            if (noteInteractor.noteType == noteData.noteType)
            {
                HandleCollision();
            }
            else
            {
                Miss();
            }
        }
    }

    private void HandleCollision()
    {
        isHit = true;

        if (hitFX != null)
        {
            Instantiate(hitFX, transform.position, Quaternion.identity);
        }

        scoreSystem.SetScore(noteScore, NoteRatings.Success);

        Destroy(gameObject);
    }

    protected void Miss()
    {
        scoreSystem.SetScore(0, NoteRatings.Miss);
        Destroy(gameObject);
    }

    public void SetHitFX(GameObject effect)
    {
        hitFX = effect;
    }
}
