using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlotMachineEffectTest : MonoBehaviour
{
    [SerializeField]
    private SlotMachineEffect slotEffect;

    private void Update()
    {
        // 1키를 누르면 첫 번째 패널 선택
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(true);
            }
        }
        // 2키를 누르면 두 번째 패널 선택
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (slotEffect != null)
            {
                slotEffect.StartSpinningWithResult(false);
            }
        }
    }
}
