using UnityEngine;

[System.Serializable]
public struct PhysicalProperty
{
    public AttributeType type;
    public float numericalValue;
    public bool isUnlocked;

    public string GetFormattedValue()
    {
        // Implementation here
        return "";
    }
}
