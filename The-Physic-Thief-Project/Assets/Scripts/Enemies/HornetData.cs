using Unity.VisualScripting;
using UnityEngine;

namespace PhysicsThief.Enemies
{
    [CreateAssetMenu(menuName = "Physics Thief/HornetData", fileName = "HornetData")]
    public class HornetData : EnemyData
    {
        [Header("Poisonous dards")]
        [SerializeField] private Projectile venomDartPrefab;

        [Header("Flight")]
        [SerializeField] private float hoverHeight = 3f;
        [SerializeField] private float preferredDistance = 8f;
        [SerializeField] private float stallFallSpeed = 12f;
        [SerializeField] private float bobAmplitude = 0.5f;
        [SerializeField] private float bobFrequency = 1f; 
        

        public Projectile VenomDartPrefab => venomDartPrefab;
        public float HoverHeight => hoverHeight;
        public float PreferredDistance => preferredDistance;
        public float StallFallSpeed => stallFallSpeed;
        public float BobAmplitude => bobAmplitude;
        public float BobFrequency => bobFrequency;

        protected override void OnValidate()
        {
            base.OnValidate();
            hoverHeight = Mathf.Max(0f, hoverHeight);
            preferredDistance = Mathf.Max(0f, preferredDistance);
            stallFallSpeed = Mathf.Max(0f, stallFallSpeed);
            bobFrequency = Mathf.Max(0f, bobFrequency);
            bobAmplitude = Mathf.Max(0f, bobAmplitude);
        }
    }
}
