using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineEffectTest : MonoBehaviour
{
    [SerializeField]
    private SlotMachineEffect slotEffect;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(false);
            }
        }
    }
}
