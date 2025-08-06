using UnityEngine;

namespace FightingFramework.Core
{
    public class FightingFrameworkServiceScope : ServiceScope
    {
        [Header("Framework Services")]
        [SerializeField] private bool debugMode = false;
        
        protected override void ConfigureServices()
        {
            if (debugMode)
            {
                Debug.Log("Configuring Fighting Framework services...");
            }
            
            // Example service registrations would go here
            // Register<IInputManager>(new InputManager());
            // Register<IHealthSystem>(new HealthSystem());
            // Register<ICombatSystem>(new CombatSystem());
            
            if (debugMode)
            {
                Debug.Log($"Fighting Framework services configured. Total services: {GetServiceCount()}");
            }
        }
        
        private int GetServiceCount()
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var field = GetType().BaseType?.GetField("services", flags);
            if (field?.GetValue(this) is System.Collections.IDictionary dict)
            {
                return dict.Count;
            }
            return 0;
        }
    }
}