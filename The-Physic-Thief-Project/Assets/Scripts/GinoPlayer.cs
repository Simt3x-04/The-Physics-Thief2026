using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(BriefcaseInventory))]
public class GinoPlayer : MonoBehaviour
{
    public float moveSpeed;
    public float jumpForce;
    
    private HealthComponent healthComp;
    private BriefcaseInventory briefcase;

    private void Move(Vector3 dir)
    {
        // Implementation here
    }

    private void Jump()
    {
        // Implementation here
    }

    private void MeleeAttack()
    {
        // Implementation here
    }

    private void EatApple()
    {
        // Implementation here
    }

    private void OnDeath()
    {
        // Implementation here
    }
}
