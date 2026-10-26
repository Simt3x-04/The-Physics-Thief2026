using UnityEngine;

[System.Serializable]
public class AppliedPropertyInstance
{
    public PhysicalProperty propertyData;
    public float remainingTime;
    public PhysicalObject targetObject;

    public void UpdateTimer(float deltaTime)
    {
        // Implementation here
    }

    public bool HasExpired()
    {
        // Implementation here
        return false;
    }
}
