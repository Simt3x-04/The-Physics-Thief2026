using UnityEngine;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.UI
{
    /// <summary>
    /// Draws a line from the object to the point where it will be launched, while the player holds the left click
    /// (velocity aiming).
    /// </summary>
    public class AimIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerScanner scanner;
        [SerializeField] private float lineWidth = 0.05f;

        private LineRenderer line;

        private void Awake()
        {
            if (scanner == null) scanner = GetComponentInParent<PlayerScanner>();
            if (scanner == null) scanner = GetComponent<PlayerScanner>();
            BuildLine();
        }

        private void OnEnable()
        {
            if (scanner == null) return;
            scanner.OnAimUpdated += HandleAimUpdated;
            scanner.OnAimEnded += HandleAimEnded;
        }

        private void OnDisable()
        {
            if (scanner != null)
            {
                scanner.OnAimUpdated -= HandleAimUpdated;
                scanner.OnAimEnded -= HandleAimEnded;
            }
            HandleAimEnded();
        }

        private void HandleAimUpdated(Vector3 from, Vector3 to)
        {
            if (line == null) return;
            line.enabled = true;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }

        private void HandleAimEnded()
        {
            if (line != null) line.enabled = false;
        }

        private void BuildLine()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("[AimIndicator] No unlit shader found: the aim line is disabled.");
                return;
            }

            var lineObject = new GameObject("AimLine");
            lineObject.transform.SetParent(transform, false);

            line = lineObject.AddComponent<LineRenderer>();
            line.material = new Material(shader);
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = AttributePalette.Get(AttributeType.Velocity);
            line.endColor = AttributePalette.Get(AttributeType.Velocity);
            line.enabled = false;
        }
    }
}
