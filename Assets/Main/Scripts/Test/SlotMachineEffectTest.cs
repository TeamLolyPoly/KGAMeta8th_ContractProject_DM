using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineEffectTest : MonoBehaviour
{
    [SerializeField]
    private SlotMachineEffect slotEffect;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(false);
            }
        }
    }
}
