using UnityEngine;
using System.Collections;
using NUnit.Framework;

namespace PhysicsThief.Enemies
{
    /// <summary>Calabrone volante che attacca a distanza con dardi di veleno.</summary>
    public class Hornet : EnemyBase
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask groundMask = -0;
        [SerializeField] private float hoverHeight = 3f;
        private const float AltitudeGain = 5f;
        private const float MaxVerticalSpeed = 6f;
        private HornetData hornetData;
        private bool isStalled;
        private float flightBaseY;
        private float bobPhase;
        private float savedSpeed;
        private float savedHeight;
        private Coroutine stallCoroutine;

        protected override void Awake()
        {
            base.Awake();
            hornetData = data as HornetData;
            if(hornetData == null){
                Debug.LogError($"No hornetData", this);
                enabled = false;
            }
            if(rb != null)rb.useGravity = false;
           flightBaseY = transform.position.y;
            Vector3 origin = transform.position + Vector3.up * 50f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f,
                                groundMask, QueryTriggerInteraction.Ignore))
            {
                flightBaseY = hit.point.y + hornetData.HoverHeight;
                Debug.Log($"[Hornet] Raycast OK su '{hit.collider.name}' ground y={hit.point.y} base={flightBaseY}", this);
            }
            else
            {
                Debug.LogWarning("[Hornet] Raycast NON ha colpito nulla: controlla Layer del Plane e Ground Mask", this);
            }
            bobPhase = Random.Range(0f, Mathf.PI*2f);
            Debug.Log($"[Hornet] hover={hornetData.HoverHeight} base={flightBaseY} startY={transform.position.y} data={hornetData.name}", this);
        }

        public override IEnemyState CreateIdleState()
        {
            return new RoamState();
        }

        private void FixedUpdate()
        {
            if (isStalled || rb == null) return;

            float bob = Mathf.Sin(Time.time * hornetData.BobFrequency * Mathf.PI * 2f + bobPhase) * hornetData.BobAmplitude;
            float targetY = flightBaseY + bob;

            Vector3 v = rb.linearVelocity;
            v.y = Mathf.Clamp((targetY - rb.position.y) * AltitudeGain, -MaxVerticalSpeed, MaxVerticalSpeed);
            rb.linearVelocity = v;
            Debug.Log($"[Hornet] y={rb.position.y:F2} targetY={targetY:F2} vy={v.y:F2}");
        }

        public override void Attack()
        {
            //TODO: if in range then attack
        }
        public void HoverOutRanged(){

        }
        public void FireVenomDart(){
            //TODO instance hornetData.VenomDartPrefab da firePoint verso target
        }

        
        public void StallAndCrash(float duration){
            if(IsDead) return;
            if(stallCoroutine != null)StopCoroutine(stallCoroutine);
            stallCoroutine = StartCoroutine(StallRoutine(duration));
        }

        IEnumerator StallRoutine(float duration)
        {
            isStalled = true;
            Stop();
            yield return new WaitForSeconds(duration);
            isStalled = false;
            stallCoroutine = null;
        }
    }
}
