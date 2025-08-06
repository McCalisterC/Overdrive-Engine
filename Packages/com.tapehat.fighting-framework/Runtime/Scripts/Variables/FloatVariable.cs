using UnityEngine;

namespace FightingFramework.Variables
{
    [CreateAssetMenu(fileName = "Float Variable", menuName = "Fighting Framework/Variables/Float")]
    public class FloatVariable : BaseVariable<float>
    {
        public void Add(float amount)
        {
            Value += amount;
        }
        
        public void Subtract(float amount)
        {
            Value -= amount;
        }
        
        public void Multiply(float amount)
        {
            Value *= amount;
        }
        
        public void Divide(float amount)
        {
            if (amount != 0)
            {
                Value /= amount;
            }
        }
        
        public void Clamp(float min, float max)
        {
            Value = Mathf.Clamp(Value, min, max);
        }
        
        public static implicit operator float(FloatVariable variable)
        {
            return variable.Value;
        }
    }
}