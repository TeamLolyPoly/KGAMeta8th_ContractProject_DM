using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField, Header("노트 점수")]
    protected int noteScore = 0;

    protected NoteData noteData;

    protected double spawnDspTime; // dspTime을 기준으로 생성 시간 저장
    public virtual void Initialize(NoteData data)
    { }

    protected void Miss()
    {
        NoteGameManager.Instance.SetScore(0, NoteRatings.Miss);
        Destroy(gameObject);
    }
}
