using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public abstract class EnemyBase : PhysicalObject
{
    protected HealthComponent healthComp;
    
    public virtual void Attack()
    {
        // Implementation here
    }

    public virtual void Die()
    {
        // Implementation here
    }
}

public class CarrotEnemy : EnemyBase
{
    public void BurrowAndCharge()
    {
        // Implementation here
    }
}

public class CornSoldier : EnemyBase
{
    public bool hasShield;

    public void FireKernelBurst()
    {
        // Implementation here
    }

    public void Defend()
    {
        // Implementation here
    }
}

public class MathExamMiniBoss : EnemyBase
{
    public void FireGradeProjectile(int grade)
    {
        // Implementation here
    }

    public void ReflectDamage(GameObject proj)
    {
        // Implementation here
    }
}

public class CrazyMachineBoss : EnemyBase
{
    public int currentPhase;
    public bool isJointsDestroyed;
    public bool isArmorStripped;

    public void SwitchPhase(int newPhase)
    {
        // Implementation here
    }

    public void VentHeat()
    {
        // Implementation here
    }

    public void TakeCoreDamage(float amount)
    {
        // Implementation here
    }
}
