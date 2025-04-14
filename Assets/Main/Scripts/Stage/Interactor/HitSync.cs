using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitSync : MonoBehaviour
{
    public ParticleSystem hitFX;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out Note note))
        {
            print("[HitSync] 파티클 재생");
            var effect = PoolManager.Instance.Spawn<ParticleSystem>(
                hitFX.gameObject,
                transform.position,
                Quaternion.identity
            );
            Destroy(note.gameObject);
            effect.Play();
            PoolManager.Instance.Despawn(effect, 2.0f);
        }
    }
}
