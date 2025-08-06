using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Animation
{
    public enum BoxType 
    { 
        Hitbox, 
        Hurtbox, 
        Collision, 
        Proximity,
        Pushbox
    }
    
    [System.Serializable]
    public struct Rectangle
    {
        [Header("Box Properties")]
        public Vector2 center;
        public Vector2 size;
        public BoxType type;
        
        [Header("Combat Properties")]
        public int damage;
        public float hitstun;
        public float blockstun;
        public float knockback;
        public Vector2 knockbackDirection;
        
        [Header("Visual")]
        public Color debugColor;
        public string label;
        
        public Rect ToRect()
        {
            return new Rect(center - size * 0.5f, size);
        }
        
        public bool Overlaps(Rectangle other)
        {
            var rect1 = ToRect();
            var rect2 = other.ToRect();
            return rect1.Overlaps(rect2);
        }
        
        public bool Contains(Vector2 point)
        {
            return ToRect().Contains(point);
        }
        
        public static Rectangle CreateHitbox(Vector2 center, Vector2 size, int damage = 10)
        {
            return new Rectangle
            {
                center = center,
                size = size,
                type = BoxType.Hitbox,
                damage = damage,
                hitstun = 15f,
                blockstun = 8f,
                knockback = 5f,
                knockbackDirection = Vector2.right,
                debugColor = Color.red,
                label = "Hitbox"
            };
        }
        
        public static Rectangle CreateHurtbox(Vector2 center, Vector2 size)
        {
            return new Rectangle
            {
                center = center,
                size = size,
                type = BoxType.Hurtbox,
                debugColor = Color.blue,
                label = "Hurtbox"
            };
        }
    }
    
    [System.Serializable]
    public enum FrameEventType
    {
        PlaySound,
        SpawnEffect,
        EnableHitbox,
        DisableHitbox,
        RootMotion,
        CameraShake,
        Custom
    }
    
    [System.Serializable]
    public struct FrameEvent
    {
        public FrameEventType eventType;
        public string eventName;
        public string stringParameter;
        public float floatParameter;
        public int intParameter;
        public Vector2 vector2Parameter;
        public UnityEngine.Object objectParameter;
        
        public static FrameEvent PlaySound(string soundName)
        {
            return new FrameEvent
            {
                eventType = FrameEventType.PlaySound,
                eventName = "PlaySound",
                stringParameter = soundName
            };
        }
        
        public static FrameEvent SpawnEffect(string effectName, Vector2 position)
        {
            return new FrameEvent
            {
                eventType = FrameEventType.SpawnEffect,
                eventName = "SpawnEffect",
                stringParameter = effectName,
                vector2Parameter = position
            };
        }
        
        public static FrameEvent Custom(string eventName, params object[] parameters)
        {
            var frameEvent = new FrameEvent
            {
                eventType = FrameEventType.Custom,
                eventName = eventName
            };
            
            // Simple parameter assignment (extend as needed)
            if (parameters.Length > 0 && parameters[0] is string str) frameEvent.stringParameter = str;
            if (parameters.Length > 1 && parameters[1] is float f) frameEvent.floatParameter = f;
            if (parameters.Length > 2 && parameters[2] is int i) frameEvent.intParameter = i;
            
            return frameEvent;
        }
    }
    
    [System.Serializable]
    public struct FrameData
    {
        [Header("Frame Properties")]
        public int frameNumber;
        public Sprite sprite;
        
        [Header("Collision Boxes")]
        public List<Rectangle> hitboxes;
        public List<Rectangle> hurtboxes;
        public List<Rectangle> collisionBoxes;
        public List<Rectangle> proximityBoxes;
        
        [Header("Movement")]
        public Vector2 rootMotion;
        public bool lockPosition;
        
        [Header("Events")]
        public List<FrameEvent> events;
        
        [Header("Visual Effects")]
        public Color spriteColor;
        public Vector2 spriteOffset;
        public Vector2 spriteScale;
        public float spriteRotation;
        
        public FrameData(int frameNumber)
        {
            this.frameNumber = frameNumber;
            sprite = null;
            hitboxes = new List<Rectangle>();
            hurtboxes = new List<Rectangle>();
            collisionBoxes = new List<Rectangle>();
            proximityBoxes = new List<Rectangle>();
            rootMotion = Vector2.zero;
            lockPosition = false;
            events = new List<FrameEvent>();
            spriteColor = Color.white;
            spriteOffset = Vector2.zero;
            spriteScale = Vector2.one;
            spriteRotation = 0f;
        }
        
        public List<Rectangle> GetBoxesByType(BoxType type)
        {
            switch (type)
            {
                case BoxType.Hitbox:
                    return hitboxes ?? new List<Rectangle>();
                case BoxType.Hurtbox:
                    return hurtboxes ?? new List<Rectangle>();
                case BoxType.Collision:
                    return collisionBoxes ?? new List<Rectangle>();
                case BoxType.Proximity:
                    return proximityBoxes ?? new List<Rectangle>();
                default:
                    return new List<Rectangle>();
            }
        }
        
        public List<Rectangle> GetAllBoxes()
        {
            var allBoxes = new List<Rectangle>();
            if (hitboxes != null) allBoxes.AddRange(hitboxes);
            if (hurtboxes != null) allBoxes.AddRange(hurtboxes);
            if (collisionBoxes != null) allBoxes.AddRange(collisionBoxes);
            if (proximityBoxes != null) allBoxes.AddRange(proximityBoxes);
            return allBoxes;
        }
        
        public bool HasEvents()
        {
            return events != null && events.Count > 0;
        }
    }
}