using UnityEngine;

namespace FightingFramework.Variables
{
    [CreateAssetMenu(fileName = "String Variable", menuName = "Fighting Framework/Variables/String")]
    public class StringVariable : BaseVariable<string>
    {
        public void Append(string text)
        {
            Value += text;
        }
        
        public void Prepend(string text)
        {
            Value = text + Value;
        }
        
        public void Clear()
        {
            Value = string.Empty;
        }
        
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        
        public int Length => Value?.Length ?? 0;
        
        public static implicit operator string(StringVariable variable)
        {
            return variable.Value;
        }
    }
}