namespace PhysicsThief.Properties
{
    /// <summary>
    /// Object material: Reactors use this to determine the reaction
    /// (e.g., temperature applied to Water -> Ice; to Wood -> fire; to Metal -> melting).
    /// </summary>
    public enum MaterialTag
    {
        Generic,
        Water,
        Ice,
        Wood,
        Metal,
        Glass,
        Soil,
        Organic
    }
}
