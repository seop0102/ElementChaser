using UnityEngine;

// 투척체 착지 후 실행할 효과
public abstract class ThrowableImpactEffect : ScriptableObject
{
    public abstract void Activate(RaycastHit landingHit);
}