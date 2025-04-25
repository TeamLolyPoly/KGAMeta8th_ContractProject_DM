using UnityEngine;

public class LongNote : Note, IPoolable
{
    public override void Initialize(NoteData data)
    {
        base.Initialize(data);
    }

    public void SetPositions(Vector3 start, Vector3 target)
    {
        startPosition = start;
        targetPosition = target;
        transform.position = start;
        transform.rotation = Quaternion.Euler(90f, 0, 0);
    }

    public void OnSpawnFromPool()
    {
        isInitialized = false;
    }

    public void OnReturnToPool()
    {
        isInitialized = false;
    }

    protected override void Miss()
    {
        base.Miss();
        PoolManager.Instance.Despawn(this);
    }
}
