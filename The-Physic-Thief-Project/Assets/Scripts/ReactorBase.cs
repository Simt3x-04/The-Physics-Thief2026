using UnityEngine;

public abstract class ReactorBase : MonoBehaviour
{
    protected PhysicalObject physObj;

    public abstract void HandlePropertyChange(AttributeType type, float newValue);
}

public class RigidbodyReactor : ReactorBase
{
    private Rigidbody rb;

    public override void HandlePropertyChange(AttributeType type, float newValue)
    {
        // Implementation here
    }

    private void UpdateMass()
    {
        // Implementation here
    }

    private void LaunchAsProjectile()
    {
        // Implementation here
    }
}

public class TemperatureReactor : ReactorBase
{
    public override void HandlePropertyChange(AttributeType type, float newValue)
    {
        // Implementation here
    }

    private void FreezeWaterToIce()
    {
        // Implementation here
    }

    private void EvaporateWater()
    {
        // Implementation here
    }

    private void IgniteWood()
    {
        // Implementation here
    }

    private void MeltMetal()
    {
        // Implementation here
    }
}

public class MagnetismReactor : ReactorBase
{
    public override void HandlePropertyChange(AttributeType type, float newValue)
    {
        // Implementation here
    }

    private void AttractMetallicObjects()
    {
        // Implementation here
    }

    private void StripEnemyShields()
    {
        // Implementation here
    }
}
