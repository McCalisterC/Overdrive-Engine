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
            return System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance 
                switch
            {
                var flags => GetType().BaseType
                    .GetField("services", flags)?
                    .GetValue(this) is System.Collections.IDictionary dict ? dict.Count : 0
            };
        }
    }
}