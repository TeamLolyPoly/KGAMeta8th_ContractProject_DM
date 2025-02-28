using UnityEngine;

public class ResourceManager<T> : Singleton<ResourceManager<T>>
    where T : MonoBehaviour
{
    public T GetResource(string name)
    {
        return Resources.Load<T>(name);
    }

    public T[] GetResources(string name)
    {
        return Resources.LoadAll<T>(name);
    }

    public T[] GetAllResources()
    {
        return Resources.LoadAll<T>("");
    }
}
