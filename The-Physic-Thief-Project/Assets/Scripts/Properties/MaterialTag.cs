namespace PhysicsThief.Properties
{
    /// <summary>
    /// Materiale di un oggetto: i Reactor lo usano per decidere la reazione
    /// (es. Temperature su Water -> Ice, su Wood -> fuoco, su Metal -> fusione).
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
