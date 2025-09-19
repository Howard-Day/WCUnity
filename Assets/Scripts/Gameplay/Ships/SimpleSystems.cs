using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface IPowerSource
{
    Capacitor MainCapacitor { get; }
}

public class Capacitor
{
    private float currentCharge;
    private float maxCharge;
    /// <summary>
    /// If true, CurrentCharge will be clamped to never
    /// exceed MaxCharge. If false, we allow CurrentCharge
    /// to exceed MaxCharge.
    /// </summary>
    private bool allowOvercharge;

    public Capacitor(float maxCharge, bool allowOvercharge)
    {
        this.maxCharge = maxCharge;
        this.currentCharge = maxCharge;
        this.allowOvercharge = allowOvercharge;
    }

    public float CurrentCharge
    {
        get => currentCharge;
        set
        {
            if (allowOvercharge)
            {
                currentCharge = Mathf.Max(0, value);
            } else
            {
                currentCharge = Mathf.Clamp(value, 0, maxCharge);
            }
        }
    }

    public float MaxCharge => maxCharge;
    public bool AllowOvercharge => allowOvercharge;
    public float CurrentChargeNormalized => currentCharge / maxCharge;
    public bool IsFull => currentCharge >= maxCharge;
    public bool IsEmpty => currentCharge <= 0;
}
