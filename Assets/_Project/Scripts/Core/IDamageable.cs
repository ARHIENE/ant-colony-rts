namespace AntColony.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        UnityEngine.Vector3 Position { get; }
        void TakeDamage(float amount);
    }
}
