using Unity.VisualScripting;
using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/HornetData", fileName = "HornetData")]
    public class HornetData : EnemyData
    {
        [Header("Poisonous dards")]
        [SerializeField] private PoisonDart venomDartPrefab;

        [Header("Flight")]
        [SerializeField] private float hoverHeight = 3f;
        [SerializeField] private float preferredDistance = 8f;
        [SerializeField] private float stallFallSpeed = 12f;
        [SerializeField] private float bobAmplitude = 0.5f;
        [SerializeField] private float bobFrequency = 1f; 
        [SerializeField] private float poisonDps = 2f;
        [SerializeField] private float poisonDuration = 5f;

        public PoisonDart VenomDartPrefab => venomDartPrefab;
        public float HoverHeight => hoverHeight;
        public float PreferredDistance => preferredDistance;
        public float StallFallSpeed => stallFallSpeed;
        public float BobAmplitude => bobAmplitude;
        public float BobFrequency => bobFrequency;
        public float PoisonDps => poisonDps;
        public float PoisonDuration => poisonDuration;

        protected override void OnValidate()
        {
            base.OnValidate();
            hoverHeight = Mathf.Max(0f, hoverHeight);
            preferredDistance = Mathf.Max(0f, preferredDistance);
            stallFallSpeed = Mathf.Max(0f, stallFallSpeed);
            bobFrequency = Mathf.Max(0f, bobFrequency);
            bobAmplitude = Mathf.Max(0f, bobAmplitude);
            poisonDps = Mathf.Max(0f, poisonDps);
            poisonDuration = Mathf.Max(0f, poisonDuration);
        }
    }
}
