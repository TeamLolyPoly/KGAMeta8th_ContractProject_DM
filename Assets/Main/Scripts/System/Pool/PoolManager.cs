using System.Collections;
using Photon.Pun;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>, IInitializable
{
    public bool IsInitialized { get; private set; }
    private static ObjectPool objectPool;
    private PhotonView photonView;

    protected override void Awake()
    {
        base.Awake();
        photonView = gameObject.AddComponent<PhotonView>();
    }

    public void Initialize()
    {
        try
        {
            InitializePool();
            IsInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing PoolManager: {e.Message}");
            IsInitialized = false;
        }
    }

    private void InitializePool()
    {
        if (objectPool == null)
        {
            GameObject poolObj = GameObject.Find("ObjectPool");
            if (poolObj == null)
            {
                poolObj = new GameObject("ObjectPool");
                DontDestroyOnLoad(poolObj);
            }
            objectPool = poolObj.GetComponent<ObjectPool>();
            if (objectPool == null)
            {
                objectPool = poolObj.AddComponent<ObjectPool>();
            }
        }
    }

    // isNetworked가 true일 경우 PhotonNetwork를 사용하여 네트워크 오브젝트 생성
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

        if (isNetworked && PhotonNetwork.IsConnected)
        {
            // 네트워크 오브젝트 생성
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
            if (obj.GetComponent<PhotonView>().IsMine) // 자신의 객체만 삭제 가능
            {
                PhotonNetwork.Destroy(obj.gameObject);
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
