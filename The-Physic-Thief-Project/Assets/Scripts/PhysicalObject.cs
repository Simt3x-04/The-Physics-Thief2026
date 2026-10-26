using UnityEngine;
using System.Collections.Generic;

public class PhysicalObject : MonoBehaviour
{
    [Header("Base Properties")]
    protected float baseMass;
    protected Vector3 baseVelocity;
    protected float baseTemperature;
    protected float baseMagnetism;
    protected float baseHardness;

    [Header("Runtime Overrides")]
    protected List<AppliedPropertyInstance> activeProperties;

    public virtual void ApplyProperty(PhysicalProperty prop)
    {
        // Implementation here
    }

    protected virtual void RemoveProperty(AppliedPropertyInstance inst)
    {
        // Implementation here
    }

    protected virtual void EvaluateThresholds()
    {
        // Implementation here
    }

    public virtual void OnTimerExpired(AppliedPropertyInstance inst)
    {
        // Implementation here
    }
}
