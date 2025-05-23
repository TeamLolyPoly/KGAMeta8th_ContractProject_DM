using UnityEngine;
using UnityEngine.VFX;

public class Note : MonoBehaviour, IInitializable
{
    [SerializeField, Header("노트 점수")]
    protected int noteScore = 100;

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

    public virtual void SetNoteColor(NoteColor color)
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

    private void Update()
    {
        if (!isInitialized)
            return;

        double elapsedTime = AudioSettings.dspTime - spawnDspTime;

        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float currentDistance = noteData.noteSpeed * (float)elapsedTime;

        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        if (progress >= 1.0f)
        {
            if (!GameManager.Instance.IsInMultiStage)
            {
                Miss();
            }
            else
            {
                if (noteData.isInteractable)
                {
                    Miss();
                }
                else
                {
                    PoolManager.Instance.Despawn(this);
                }
            }
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

    protected virtual void OnCollisionEnter(Collision other)
    {
        if (!isInitialized)
            return;
        if (!GameManager.Instance.IsInMultiStage)
        {
            if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
            {
                if (noteInteractor.noteColor == noteData.noteColor)
                {
                    HandleCollision();
                    noteInteractor?.SendImpulse();
                }
                else
                {
                    Miss();
                }
            }
        }
        else
        {
            if (noteData.isInteractable)
            {
                if (other.gameObject.TryGetComponent(out NoteInteractor noteInteractor))
                {
                    if (noteInteractor.noteColor == noteData.noteColor)
                    {
                        HandleCollision();
                        noteInteractor?.SendImpulse();
                    }
                    else
                    {
                        Miss();
                    }
                }
            }
        }
    }

    private void HandleCollision()
    {
        if (!isInitialized || isHit)
            return;

        isHit = true;

        if (hitFX != null)
        {
            VisualEffect hitFXInstance = PoolManager.Instance.Spawn<VisualEffect>(
                hitFX,
                transform.position,
                Quaternion.identity
            );
            PoolManager.Instance.Despawn(hitFXInstance, 0.5f);
        }

        scoreSystem.SetScore(noteScore, NoteRatings.Success);

        PoolManager.Instance.Despawn(this);
    }

    protected virtual void Miss()
    {
        if (!isHit)
        {
            isHit = true;
            if (scoreSystem == null)
            {
                scoreSystem = GameManager.Instance.ScoreSystem;
            }
            if (scoreSystem != null)
            {
                scoreSystem.SetScore(0, NoteRatings.Miss);
            }
        }

        PoolManager.Instance.Despawn(this);
    }

    public void SetHitFX(GameObject effect)
    {
        hitFX = effect;
    }
}
