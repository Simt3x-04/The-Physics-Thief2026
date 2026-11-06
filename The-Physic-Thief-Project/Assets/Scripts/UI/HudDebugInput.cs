using UnityEngine;
using UnityEngine.InputSystem;
using PhysicsThief.Core;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// TEMPORARY: test the HUD without a player character.
    /// H = -10 HP | J = +25 HP | F1..F5 = copy Mass/Velocity/Temperature/Magnetism/Hardness | 1..5 = activate + select slot
    /// Space = use selected slots | X = discard active slot | U = unlock +1 slot | K = unlock Temperature and Magnetism
    /// </summary>
    public class HudDebugInput : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private BriefcaseInventory inventory;
        [SerializeField] private HUDManager hud;

        private static readonly Key[] CopyKeys = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5 };
        private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        private static readonly float[] SampleValues = { 600f, 30f, 0f, 1f, 8f };   // a value for every AttributeType

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            BriefcaseModel model = inventory.Model;

            if (kb[Key.H].wasPressedThisFrame) health.TakeDamage(10f);
            if (kb[Key.J].wasPressedThisFrame) health.Heal(25f);

            for (int i = 0; i < CopyKeys.Length; i++)
            {
                if (kb[CopyKeys[i]].wasPressedThisFrame)
                {
                    hud.ShowCopyResult(model.TryAdd(new PropertyValue((AttributeType)i, SampleValues[i])));
                }
                if (kb[SlotKeys[i]].wasPressedThisFrame)
                {
                    model.SetActiveSlot(i);
                    model.ToggleSelected(i);
                }
            }

            if (kb[Key.Space].wasPressedThisFrame) model.ConsumeSelected();
            if (kb[Key.X].wasPressedThisFrame) model.TrashActive();
            if (kb[Key.U].wasPressedThisFrame) model.UnlockSlots(model.UnlockedSlots + 1);
            if (kb[Key.K].wasPressedThisFrame)
            {
                model.UnlockAttribute(AttributeType.Temperature);
                model.UnlockAttribute(AttributeType.Magnetism);
            }
        }
    }
}
