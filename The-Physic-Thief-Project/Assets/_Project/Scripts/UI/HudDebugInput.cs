using UnityEngine;
using UnityEngine.InputSystem;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// TEMPORARY: health and unlock testing, independent of the rest of the game. REMOVE once integrated into the game.
    /// H = -10 HP | J = +25 HP | Q or U = unlock +1 slot | K = unlock Temperature and Magnetism
    /// (Slot copying, selection, usage, and discarding are now handled by the PlayerScanner.)
    /// </summary>
    public class HudDebugInput : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private BriefcaseInventory inventory;

        private HUDManager hud;

        private void Notify(string message)
        {
            if (hud == null) hud = FindFirstObjectByType<HUDManager>();
            if (hud != null) hud.ShowMessage(message);
            Debug.Log("[HudDebugInput] " + message);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            BriefcaseModel model = inventory.Model;

            if (kb[Key.H].wasPressedThisFrame) health.TakeDamage(10f);
            if (kb[Key.J].wasPressedThisFrame) health.Heal(25f);
            if (kb[Key.Q].wasPressedThisFrame || kb[Key.U].wasPressedThisFrame)
            {
                model.UnlockSlots(model.UnlockedSlots + 1);
                Notify("Slots unlocked: " + model.UnlockedSlots + "/" + BriefcaseModel.MaxSlotCount);
            }
            if (kb[Key.K].wasPressedThisFrame)
            {
                model.UnlockAttribute(AttributeType.Temperature);
                model.UnlockAttribute(AttributeType.Magnetism);
                Notify("Temperature and Magnetism unlocked");
            }
        }
    }
}
