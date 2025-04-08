using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PoolManager : Singleton<PoolManager>, IInitializable
{
    public bool IsInitialized { get; private set; }
    private static ObjectPool objectPool;

    public void Initialize()
    {
        objectPool = new GameObject("ObjectPool").AddComponent<ObjectPool>();
        DontDestroyOnLoad(objectPool);

        SceneManager.sceneUnloaded += OnSceneUnloaded;

        IsInitialized = true;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ClearAllPools();
    }

    public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation)
        where T : Component
    {
        string originalName = prefab.name;
        T spawnedObj;

        spawnedObj = objectPool.Spawn<T>(prefab, position, rotation);

        if (spawnedObj != null)
            spawnedObj.gameObject.name = originalName;

        return spawnedObj;
    }

    public void Despawn<T>(T obj)
        where T : Component
    {
        if (!IsInitialized || obj == null)
        {
            Debug.LogError("PoolManager is not initialized or object is null! Cannot despawn.");
            return;
        }

        objectPool.Despawn(obj);
    }

    public void Despawn<T>(T obj, float delay)
        where T : Component
    {
        if (!IsInitialized || obj == null)
        {
            Debug.LogError("PoolManager is not initialized or object is null! Cannot despawn.");
            return;
        }

        if (delay > 0)
        {
            StartCoroutine(DespawnCoroutine(obj, delay));
        }
        else
        {
            Despawn(obj);
        }
    }

    private IEnumerator DespawnCoroutine<T>(T obj, float delay)
        where T : Component
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            Despawn(obj);
        }
    }

    public void ClearAllPools()
    {
        if (objectPool != null)
        {
            objectPool.ClearAllPools();
        }
    }
}
