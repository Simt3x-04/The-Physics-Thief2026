using UnityEngine;
using PhysicsThief.Core;

namespace PhysicsThief.Enemies
{
    /// <summary>For testing only: shows the current state and HP above the enemy (visible in the Game view).</summary>
    [RequireComponent(typeof(EnemyBase))]
    public class EnemyDebugLabel : MonoBehaviour
    {
        private EnemyBase enemy;
        private HealthComponent health;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            health = GetComponent<HealthComponent>();
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (screen.z < 0f) return;

            string state = enemy.StateMachine.Current != null
                ? enemy.StateMachine.Current.GetType().Name
                : "(none)";
            string hp = health != null ? $"{health.CurrentHP}/{health.MaxHP}" : "?";

            GUI.Label(new Rect(screen.x - 60f, Screen.height - screen.y, 160f, 40f), $"{state}\nHP {hp}");
        }
    }
}