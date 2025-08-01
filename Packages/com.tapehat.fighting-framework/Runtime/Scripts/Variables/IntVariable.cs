using UnityEngine;

namespace FightingFramework.Variables
{
    [CreateAssetMenu(fileName = "Int Variable", menuName = "Fighting Framework/Variables/Int")]
    public class IntVariable : BaseVariable<int>
    {
        public void Add(int amount)
        {
            Value += amount;
        }
        
        public void Subtract(int amount)
        {
            Value -= amount;
        }
        
        public void Multiply(int amount)
        {
            Value *= amount;
        }
        
        public void Divide(int amount)
        {
            if (amount != 0)
            {
                Value /= amount;
            }
        }
        
        public void Clamp(int min, int max)
        {
            Value = Mathf.Clamp(Value, min, max);
        }
        
        public static implicit operator int(IntVariable variable)
        {
            return variable.Value;
        }
    }
}