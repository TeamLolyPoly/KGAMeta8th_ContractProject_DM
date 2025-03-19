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

    public void SetNoteColor(NoteColor color)
    {
        noteData.noteColor = color;

        if (Rim == null)
        {
            Debug.LogWarning($"노트의 Rim이 null입니다. 노트 타입: {noteData?.noteType}");
            return;
        }

        Renderer rimRenderer = Rim.GetComponent<Renderer>();
        if (rimRenderer == null)
        {
            Debug.LogWarning("Rim에 Renderer 컴포넌트가 없습니다.");
            return;
        }

        switch (color)
        {
            case NoteColor.Red:
                rimRenderer.material.color = Color.red;
                break;
            case NoteColor.Blue:
                rimRenderer.material.color = Color.blue;
                break;
            case NoteColor.Yellow:
                rimRenderer.material.color = Color.yellow;
                break;
            default:
                rimRenderer.material.color = Color.white;
                break;
        }
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

        switch (data.noteType)
        {
            case NoteType.Short:
                notePrefab = shortNotePrefab;
                break;
            case NoteType.Long:
                notePrefab = longNotePrefab;
                break;
            default:
                Debug.LogError($"Unsupported note type: {data.noteType}");
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
            if (noteInteractor.noteColor == noteData.noteColor)
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
