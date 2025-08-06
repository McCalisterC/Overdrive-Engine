using UnityEngine;

namespace FightingFramework.Variables
{
    public abstract class BaseVariable<T> : ScriptableObject
    {
        [SerializeField] protected T defaultValue;
        [SerializeField] protected T runtimeValue;
        
        public virtual T Value
        {
            get => runtimeValue;
            set => runtimeValue = value;
        }
        
        protected virtual void OnEnable()
        {
            ResetToDefault();
        }
        
        public virtual void ResetToDefault()
        {
            runtimeValue = defaultValue;
        }
        
        public virtual void SetDefaultValue(T value)
        {
            defaultValue = value;
            ResetToDefault();
        }
    }
}