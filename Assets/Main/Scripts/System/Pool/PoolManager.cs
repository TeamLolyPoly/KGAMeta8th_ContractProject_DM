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

    public T Spawn<T>(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        bool isNetworked = false
    )
        where T : Component
    {
        string originalName = prefab.name;
        T spawnedObj;

        if (isNetworked && PhotonNetwork.InRoom)
        {
            GameObject networkedObj = PhotonNetwork.Instantiate(
                $"Items/{originalName}",
                position,
                rotation
            );
            spawnedObj = networkedObj.GetComponent<T>();
        }
        else
            spawnedObj = objectPool.Spawn<T>(prefab, position, rotation);

        if (spawnedObj != null)
            spawnedObj.gameObject.name = originalName;

        return spawnedObj;
    }

    public void Despawn<T>(T obj, bool isNetworked = false)
        where T : Component
    {
        if (!IsInitialized || obj == null)
        {
            Debug.LogError("PoolManager is not initialized or object is null! Cannot despawn.");
            return;
        }

        if (isNetworked && PhotonNetwork.IsConnected)
        {
            if (obj.GetComponent<PhotonView>().IsMine)
            {
                objectPool.Despawn(obj);
            }
        }
        else
            objectPool.Despawn(obj);
    }

    public void Despawn<T>(T obj, float delay, bool isNetworked = false)
        where T : Component
    {
        if (!IsInitialized || obj == null)
        {
            Debug.LogError("PoolManager is not initialized or object is null! Cannot despawn.");
            return;
        }

        if (delay > 0)
        {
            StartCoroutine(DespawnCoroutine(obj, delay, isNetworked));
        }
        else
        {
            Despawn(obj, isNetworked);
        }
    }

    private IEnumerator DespawnCoroutine<T>(T obj, float delay, bool isNetworked)
        where T : Component
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            Despawn(obj, isNetworked);
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
