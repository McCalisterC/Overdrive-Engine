using UnityEngine;

namespace FightingFramework.Variables
{
    [CreateAssetMenu(fileName = "Bool Variable", menuName = "Fighting Framework/Variables/Bool")]
    public class BoolVariable : BaseVariable<bool>
    {
        public void Toggle()
        {
            Value = !Value;
        }
        
        public void SetTrue()
        {
            Value = true;
        }
        
        public void SetFalse()
        {
            Value = false;
        }
        
        public static implicit operator bool(BoolVariable variable)
        {
            return variable.Value;
        }
    }
}