namespace PhysicsThief.Player
{
    /// <summary>
    /// Anything the player uses with E (GDD 3.7: pick apples, open doors, use switches).
    /// PlayerInteractor finds it under the crosshair, shows its prompt and calls Interact once per E press.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>False hides the prompt and ignores E (e.g. an apple already eaten).</summary>
        bool CanInteract(PlayerController player);

        /// <summary>Text under the crosshair, e.g. "E — Eat apple (+25 HP)".</summary>
        string GetPrompt(PlayerController player);

        /// <summary>Performs the action. Returns a short message for the HUD, or null for none.</summary>
        string Interact(PlayerController player);
    }
}
