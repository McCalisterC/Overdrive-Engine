using UnityEngine;
using UnityEditor;
using FightingFramework.Animation;
using System.Collections.Generic;

namespace FightingFramework.Editor
{
    [CustomEditor(typeof(AnimationData))]
    public class AnimationDataEditor : UnityEditor.Editor
    {
        private AnimationData animData;
        private int selectedFrame = 0;
        private Vector2 scrollPosition;
        private bool showFrameList = true;
        private bool showBoxEditor = true;
        private bool showEventEditor = false;
        
        // Box editing
        private BoxType selectedBoxType = BoxType.Hitbox;
        private Rectangle editingRectangle;
        private bool isEditingBox = false;
        
        // Visual settings
        private float timelineHeight = 100f;
        private float frameWidth = 60f;
        private Color[] boxColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };
        
        private void OnEnable()
        {
            animData = (AnimationData)target;
            if (animData.frames.Count > 0)
            {
                selectedFrame = Mathf.Clamp(selectedFrame, 0, animData.frames.Count - 1);
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            DrawTimeline();
            DrawFrameEditor();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Animation Data Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic properties
            animData.animationName = EditorGUILayout.TextField("Animation Name", animData.animationName);
            animData.frameRate = EditorGUILayout.FloatField("Frame Rate", animData.frameRate);
            animData.looping = EditorGUILayout.Toggle("Looping", animData.looping);
            
            if (animData.looping)
            {
                animData.loopStartFrame = EditorGUILayout.IntSlider("Loop Start Frame", animData.loopStartFrame, 0, Mathf.Max(0, animData.totalFrames - 1));
            }
            
            EditorGUILayout.Space();
            
            // Animation info
            EditorGUILayout.LabelField($"Total Frames: {animData.totalFrames}");
            EditorGUILayout.LabelField($"Duration: {animData.duration:F2} seconds");
            
            EditorGUILayout.Space();
            
            // Quick actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Frame"))
            {
                AddFrame();
            }
            if (GUILayout.Button("Remove Frame") && animData.totalFrames > 0)
            {
                RemoveFrame(selectedFrame);
            }
            if (GUILayout.Button("Duplicate Frame") && animData.totalFrames > 0)
            {
                DuplicateFrame(selectedFrame);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawTimeline()
        {
            showFrameList = EditorGUILayout.Foldout(showFrameList, "Frame Timeline", true);
            if (!showFrameList) return;
            
            var timelineRect = GUILayoutUtility.GetRect(0, timelineHeight, GUILayout.ExpandWidth(true));
            DrawTimelineBackground(timelineRect);
            
            if (animData.totalFrames > 0)
            {
                DrawFrameThumbnails(timelineRect);
                HandleTimelineInput(timelineRect);
            }
            
            // Frame navigation
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀◀"))
            {
                selectedFrame = 0;
            }
            if (GUILayout.Button("◀"))
            {
                selectedFrame = Mathf.Max(0, selectedFrame - 1);
            }
            
            selectedFrame = EditorGUILayout.IntSlider(selectedFrame, 0, Mathf.Max(0, animData.totalFrames - 1));
            
            if (GUILayout.Button("▶"))
            {
                selectedFrame = Mathf.Min(animData.totalFrames - 1, selectedFrame + 1);
            }
            if (GUILayout.Button("▶▶"))
            {
                selectedFrame = Mathf.Max(0, animData.totalFrames - 1);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTimelineBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            // Draw frame markers
            int visibleFrames = Mathf.FloorToInt(rect.width / frameWidth);
            for (int i = 0; i < visibleFrames && i < animData.totalFrames; i++)
            {
                var frameRect = new Rect(rect.x + i * frameWidth, rect.y, frameWidth, rect.height);
                
                // Highlight selected frame
                if (i == selectedFrame)
                {
                    EditorGUI.DrawRect(frameRect, new Color(0.3f, 0.6f, 1f, 0.3f));
                }
                
                // Frame border
                EditorGUI.DrawRect(new Rect(frameRect.x, frameRect.y, 1, frameRect.height), Color.gray);
                
                // Frame number
                GUI.Label(new Rect(frameRect.x + 2, frameRect.y + 2, frameRect.width, 20), i.ToString(), EditorStyles.miniLabel);
            }
        }
        
        private void DrawFrameThumbnails(Rect timelineRect)
        {
            for (int i = 0; i < animData.totalFrames && i * frameWidth < timelineRect.width; i++)
            {
                var frameRect = new Rect(timelineRect.x + i * frameWidth + 2, timelineRect.y + 20, frameWidth - 4, timelineRect.height - 40);
                var frame = animData.GetFrame(i);
                
                // Draw sprite thumbnail
                if (frame.sprite != null)
                {
                    var spriteRect = new Rect(frameRect.x, frameRect.y, frameRect.width, frameRect.height * 0.7f);
                    DrawSpriteThumbnail(spriteRect, frame.sprite);
                }
                
                // Draw hitbox indicators
                var indicatorRect = new Rect(frameRect.x, frameRect.y + frameRect.height * 0.7f, frameRect.width, frameRect.height * 0.3f);
                DrawBoxIndicators(indicatorRect, frame);
            }
        }
        
        private void DrawSpriteThumbnail(Rect rect, Sprite sprite)
        {
            if (sprite?.texture != null)
            {
                var spriteRect = sprite.rect;
                var uv = new Rect(
                    spriteRect.x / sprite.texture.width,
                    spriteRect.y / sprite.texture.height,
                    spriteRect.width / sprite.texture.width,
                    spriteRect.height / sprite.texture.height
                );
                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
            }
        }
        
        private void DrawBoxIndicators(Rect rect, FrameData frame)
        {
            float indicatorWidth = rect.width / 4f;
            
            // Hitboxes
            if (frame.hitboxes?.Count > 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, indicatorWidth, rect.height), Color.red);
            }
            
            // Hurtboxes
            if (frame.hurtboxes?.Count > 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + indicatorWidth, rect.y, indicatorWidth, rect.height), Color.blue);
            }
            
            // Events
            if (frame.events?.Count > 0)
            {
                EditorGUI.DrawRect(new Rect(rect.x + indicatorWidth * 2, rect.y, indicatorWidth, rect.height), Color.yellow);
            }
            
            // Root motion
            if (frame.rootMotion != Vector2.zero)
            {
                EditorGUI.DrawRect(new Rect(rect.x + indicatorWidth * 3, rect.y, indicatorWidth, rect.height), Color.green);
            }
        }
        
        private void HandleTimelineInput(Rect rect)
        {
            var e = Event.current;
            
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                int clickedFrame = Mathf.FloorToInt((e.mousePosition.x - rect.x) / frameWidth);
                if (clickedFrame >= 0 && clickedFrame < animData.totalFrames)
                {
                    selectedFrame = clickedFrame;
                    Repaint();
                }
            }
        }
        
        private void DrawFrameEditor()
        {
            if (animData.totalFrames == 0)
            {
                EditorGUILayout.HelpBox("No frames in animation. Add frames to begin editing.", MessageType.Info);
                return;
            }
            
            var frame = animData.GetFrame(selectedFrame);
            
            EditorGUILayout.LabelField($"Editing Frame {selectedFrame}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Sprite assignment
            var newSprite = EditorGUILayout.ObjectField("Sprite", frame.sprite, typeof(Sprite), false) as Sprite;
            if (newSprite != frame.sprite)
            {
                frame.sprite = newSprite;
                animData.frames[selectedFrame] = frame;
                EditorUtility.SetDirty(animData);
            }
            
            // Root motion
            var newRootMotion = EditorGUILayout.Vector2Field("Root Motion", frame.rootMotion);
            if (newRootMotion != frame.rootMotion)
            {
                frame.rootMotion = newRootMotion;
                animData.frames[selectedFrame] = frame;
                EditorUtility.SetDirty(animData);
            }
            
            frame.lockPosition = EditorGUILayout.Toggle("Lock Position", frame.lockPosition);
            
            EditorGUILayout.Space();
            
            // Visual properties
            EditorGUILayout.LabelField("Visual Properties", EditorStyles.boldLabel);
            frame.spriteColor = EditorGUILayout.ColorField("Sprite Color", frame.spriteColor);
            frame.spriteOffset = EditorGUILayout.Vector2Field("Sprite Offset", frame.spriteOffset);
            frame.spriteScale = EditorGUILayout.Vector2Field("Sprite Scale", frame.spriteScale);
            frame.spriteRotation = EditorGUILayout.FloatField("Sprite Rotation", frame.spriteRotation);
            
            EditorGUILayout.Space();
            
            // Box editor
            DrawBoxEditor(ref frame);
            
            EditorGUILayout.Space();
            
            // Event editor
            DrawEventEditor(ref frame);
            
            // Update frame data
            animData.frames[selectedFrame] = frame;
        }
        
        private void DrawBoxEditor(ref FrameData frame)
        {
            showBoxEditor = EditorGUILayout.Foldout(showBoxEditor, "Collision Boxes", true);
            if (!showBoxEditor) return;
            
            // Box type selector
            selectedBoxType = (BoxType)EditorGUILayout.EnumPopup("Box Type", selectedBoxType);
            
            var boxes = frame.GetBoxesByType(selectedBoxType);
            
            EditorGUILayout.LabelField($"{selectedBoxType} Boxes ({boxes.Count})", EditorStyles.boldLabel);
            
            // Add box button
            if (GUILayout.Button($"Add {selectedBoxType}"))
            {
                AddBoxToFrame(ref frame, selectedBoxType);
            }
            
            // List existing boxes
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginVertical("box");
                
                var box = boxes[i];
                EditorGUILayout.LabelField($"{selectedBoxType} {i}", EditorStyles.boldLabel);
                
                box.center = EditorGUILayout.Vector2Field("Center", box.center);
                box.size = EditorGUILayout.Vector2Field("Size", box.size);
                box.label = EditorGUILayout.TextField("Label", box.label);
                
                if (selectedBoxType == BoxType.Hitbox)
                {
                    box.damage = EditorGUILayout.IntField("Damage", box.damage);
                    box.hitstun = EditorGUILayout.FloatField("Hitstun", box.hitstun);
                    box.blockstun = EditorGUILayout.FloatField("Blockstun", box.blockstun);
                    box.knockback = EditorGUILayout.FloatField("Knockback", box.knockback);
                    box.knockbackDirection = EditorGUILayout.Vector2Field("Knockback Direction", box.knockbackDirection);
                }
                
                box.debugColor = EditorGUILayout.ColorField("Debug Color", box.debugColor);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove"))
                {
                    RemoveBoxFromFrame(ref frame, selectedBoxType, i);
                }
                EditorGUILayout.EndHorizontal();
                
                // Update the box in the list
                boxes[i] = box;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            // Update frame with modified boxes
            UpdateFrameBoxes(ref frame, selectedBoxType, boxes);
        }
        
        private void DrawEventEditor(ref FrameData frame)
        {
            showEventEditor = EditorGUILayout.Foldout(showEventEditor, $"Frame Events ({frame.events?.Count ?? 0})", true);
            if (!showEventEditor) return;
            
            if (frame.events == null)
            {
                frame.events = new List<FrameEvent>();
            }
            
            if (GUILayout.Button("Add Event"))
            {
                frame.events.Add(new FrameEvent { eventType = FrameEventType.Custom, eventName = "NewEvent" });
            }
            
            for (int i = frame.events.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginVertical("box");
                
                var frameEvent = frame.events[i];
                
                frameEvent.eventType = (FrameEventType)EditorGUILayout.EnumPopup("Event Type", frameEvent.eventType);
                frameEvent.eventName = EditorGUILayout.TextField("Event Name", frameEvent.eventName);
                
                // Show relevant parameters based on event type
                switch (frameEvent.eventType)
                {
                    case FrameEventType.PlaySound:
                        frameEvent.stringParameter = EditorGUILayout.TextField("Sound Name", frameEvent.stringParameter);
                        break;
                    case FrameEventType.SpawnEffect:
                        frameEvent.stringParameter = EditorGUILayout.TextField("Effect Name", frameEvent.stringParameter);
                        frameEvent.vector2Parameter = EditorGUILayout.Vector2Field("Position", frameEvent.vector2Parameter);
                        break;
                    case FrameEventType.CameraShake:
                        frameEvent.floatParameter = EditorGUILayout.FloatField("Intensity", frameEvent.floatParameter);
                        break;
                    case FrameEventType.Custom:
                        frameEvent.stringParameter = EditorGUILayout.TextField("String Parameter", frameEvent.stringParameter);
                        frameEvent.floatParameter = EditorGUILayout.FloatField("Float Parameter", frameEvent.floatParameter);
                        frameEvent.intParameter = EditorGUILayout.IntField("Int Parameter", frameEvent.intParameter);
                        frameEvent.vector2Parameter = EditorGUILayout.Vector2Field("Vector2 Parameter", frameEvent.vector2Parameter);
                        break;
                }
                
                if (GUILayout.Button("Remove Event"))
                {
                    frame.events.RemoveAt(i);
                }
                
                frame.events[i] = frameEvent;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        private void AddFrame()
        {
            var newFrame = new FrameData(animData.totalFrames);
            animData.AddFrame(newFrame);
            selectedFrame = animData.totalFrames - 1;
            EditorUtility.SetDirty(animData);
        }
        
        private void RemoveFrame(int index)
        {
            animData.RemoveFrame(index);
            selectedFrame = Mathf.Clamp(selectedFrame, 0, animData.totalFrames - 1);
            EditorUtility.SetDirty(animData);
        }
        
        private void DuplicateFrame(int index)
        {
            animData.DuplicateFrame(index);
            selectedFrame = index + 1;
            EditorUtility.SetDirty(animData);
        }
        
        private void AddBoxToFrame(ref FrameData frame, BoxType boxType)
        {
            var newBox = boxType == BoxType.Hitbox ? 
                Rectangle.CreateHitbox(Vector2.zero, Vector2.one) : 
                Rectangle.CreateHurtbox(Vector2.zero, Vector2.one);
                
            switch (boxType)
            {
                case BoxType.Hitbox:
                    if (frame.hitboxes == null) frame.hitboxes = new List<Rectangle>();
                    frame.hitboxes.Add(newBox);
                    break;
                case BoxType.Hurtbox:
                    if (frame.hurtboxes == null) frame.hurtboxes = new List<Rectangle>();
                    frame.hurtboxes.Add(newBox);
                    break;
                case BoxType.Collision:
                    if (frame.collisionBoxes == null) frame.collisionBoxes = new List<Rectangle>();
                    frame.collisionBoxes.Add(newBox);
                    break;
                case BoxType.Proximity:
                    if (frame.proximityBoxes == null) frame.proximityBoxes = new List<Rectangle>();
                    frame.proximityBoxes.Add(newBox);
                    break;
            }
            
            EditorUtility.SetDirty(animData);
        }
        
        private void RemoveBoxFromFrame(ref FrameData frame, BoxType boxType, int index)
        {
            switch (boxType)
            {
                case BoxType.Hitbox:
                    if (frame.hitboxes != null && index < frame.hitboxes.Count)
                        frame.hitboxes.RemoveAt(index);
                    break;
                case BoxType.Hurtbox:
                    if (frame.hurtboxes != null && index < frame.hurtboxes.Count)
                        frame.hurtboxes.RemoveAt(index);
                    break;
                case BoxType.Collision:
                    if (frame.collisionBoxes != null && index < frame.collisionBoxes.Count)
                        frame.collisionBoxes.RemoveAt(index);
                    break;
                case BoxType.Proximity:
                    if (frame.proximityBoxes != null && index < frame.proximityBoxes.Count)
                        frame.proximityBoxes.RemoveAt(index);
                    break;
            }
            
            EditorUtility.SetDirty(animData);
        }
        
        private void UpdateFrameBoxes(ref FrameData frame, BoxType boxType, List<Rectangle> boxes)
        {
            switch (boxType)
            {
                case BoxType.Hitbox:
                    frame.hitboxes = new List<Rectangle>(boxes);
                    break;
                case BoxType.Hurtbox:
                    frame.hurtboxes = new List<Rectangle>(boxes);
                    break;
                case BoxType.Collision:
                    frame.collisionBoxes = new List<Rectangle>(boxes);
                    break;
                case BoxType.Proximity:
                    frame.proximityBoxes = new List<Rectangle>(boxes);
                    break;
            }
            
            EditorUtility.SetDirty(animData);
        }
    }
}