using UnityEngine;

namespace PhysicsThief.Enemies
{
    /// <summary>Calabrone volante che attacca a distanza con dardi di veleno.</summary>
    public class Hornet : EnemyBase
    {
        [SerializeField] private Projectile venomDartPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float hoverHeight = 3f;
        private HornetData hornetData;
        private bool isStalled;

        protected override void Awake()
        {
            base.Awake();
            hornetData = data as HornetData;
            if(hornetData == null){
                Debug.LogError($"No hornetData", this);
                enabled = false;
            }
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

        
        public void StallAndCrash(){
            /// <summary>TODO: il calabrone perde quota e precipita (es. dopo un cambio di proprieta').</summary>
        }
    }
}
