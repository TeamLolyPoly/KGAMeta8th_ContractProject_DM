using UnityEngine;

public class Note : MonoBehaviour, IInitializable
{
    protected int noteScore = 0;
    protected NoteData noteData;
    protected double spawnDspTime;
    public ParticleSystem hitFX;

    protected bool isInitialized = false;
    protected bool isHit = false;
    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        isInitialized = true;
    }

    public virtual void Initialize(NoteData data)
    {
        noteData = data;

        isInitialized = true;

        transform.LookAt(noteData.targetPosition);

        spawnDspTime = AudioSettings.dspTime;
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

        if (NoteGameManager.Instance != null)
        {
            NoteGameManager.Instance.SetScore(noteScore, NoteRatings.Success);
        }

        Destroy(gameObject);
    }

    protected void Miss()
    {
        NoteGameManager.Instance.SetScore(0, NoteRatings.Miss);
        Destroy(gameObject);
    }

    public void SetHitFX(ParticleSystem effect)
    {
        hitFX = effect;
    }
}
