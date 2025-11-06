using OneManEscapePlan.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FormationTemplate : MonoBehaviour
{
    [SerializeField, Required] private string displayName;
    [SerializeField, NonNull] private Transform[] slots;
   
    public int MaxShips => slots.Length;
    public string DisplayName => displayName;
    public IReadOnlyList<Transform> Slots => slots;

    void Awake()
    {
        Assert.IsFalse(slots.Length == 0);
    }

    public Transform GetSlot(int index)
    {
        return slots[index];
    }

    public Vector3 GetLocalOffset(int slot1Index, int slot2Index)
    {
        return slots[slot2Index].localPosition - slots[slot1Index].localPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, .5f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Gizmos.DrawSphere(slots[i].position, 2.5f);
            }
        }
    }
#endif
}
