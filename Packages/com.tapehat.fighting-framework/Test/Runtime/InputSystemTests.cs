using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using FightingFramework.Input;
using FightingFramework.Utilities;

namespace FightingFramework.Tests
{
    public class CircularBufferTests
    {
        [Test]
        public void CircularBuffer_Constructor_SetsCapacityCorrectly()
        {
            var buffer = new CircularBuffer<int>(5);
            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.IsEmpty);
            Assert.IsFalse(buffer.IsFull);
        }
        
        [Test]
        public void CircularBuffer_Add_AddsItemsCorrectly()
        {
            var buffer = new CircularBuffer<int>(3);
            
            buffer.Add(1);
            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(1, buffer.GetMostRecent());
            
            buffer.Add(2);
            buffer.Add(3);
            Assert.AreEqual(3, buffer.Count);
            Assert.IsTrue(buffer.IsFull);
            Assert.AreEqual(3, buffer.GetMostRecent());
        }
        
        [Test]
        public void CircularBuffer_OverwritesOldestWhenFull()
        {
            var buffer = new CircularBuffer<int>(3);
            
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // Should overwrite 1
            
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(4, buffer.GetMostRecent());
            Assert.AreEqual(2, buffer.GetOldest());
        }
        
        [Test]
        public void CircularBuffer_GetRecentItems_ReturnsCorrectOrder()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            
            var recent = buffer.GetRecentItems(2);
            Assert.AreEqual(2, recent.Count);
            Assert.AreEqual(3, recent[0]); // Most recent first
            Assert.AreEqual(2, recent[1]);
        }
    }
    
    public class InputCommandTests
    {
        [Test]
        public void InputCommand_CreateDirectionInput_SetsPropertiesCorrectly()
        {
            var command = InputCommand.CreateDirectionInput(InputDirection.Right, Vector2.right);
            
            Assert.AreEqual(InputType.Direction, command.type);
            Assert.AreEqual(InputDirection.Right, command.inputDirection);
            Assert.AreEqual(Vector2.right, command.direction);
        }
        
        [Test]
        public void InputCommand_CreateButtonInput_SetsPropertiesCorrectly()
        {
            var command = InputCommand.CreateButtonInput(InputButton.Light, true);
            
            Assert.AreEqual(InputType.Button, command.type);
            Assert.AreEqual(InputButton.Light, command.button);
            Assert.IsTrue(command.isPressed);
        }
        
        [Test]
        public void InputCommand_CreateNeutralInput_SetsPropertiesCorrectly()
        {
            var command = InputCommand.CreateNeutralInput();
            
            Assert.AreEqual(InputType.Neutral, command.type);
            Assert.AreEqual(InputDirection.Neutral, command.inputDirection);
            Assert.AreEqual(InputButton.None, command.button);
        }
        
        [Test]
        public void InputCommand_ToString_ReturnsReadableFormat()
        {
            var dirCommand = InputCommand.CreateDirectionInput(InputDirection.Down, Vector2.down);
            var btnCommand = InputCommand.CreateButtonInput(InputButton.Heavy, true);
            
            string dirString = dirCommand.ToString();
            string btnString = btnCommand.ToString();
            
            Assert.IsTrue(dirString.Contains("Dir(Down)"));
            Assert.IsTrue(btnString.Contains("Btn(Heavy+)"));
        }
    }
    
    public class MotionInputTests
    {
        [Test]
        public void MotionInput_IsValid_ReturnsTrueWithDirections()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.Right };
            
            Assert.IsTrue(motion.IsValid());
        }
        
        [Test]
        public void MotionInput_IsValid_ReturnsTrueWithButtons()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.buttons = new List<InputButton> { InputButton.Light };
            
            Assert.IsTrue(motion.IsValid());
        }
        
        [Test]
        public void MotionInput_IsValid_ReturnsFalseWhenEmpty()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.directions = new List<InputDirection>();
            motion.buttons = new List<InputButton>();
            
            Assert.IsFalse(motion.IsValid());
        }
        
        [Test]
        public void MotionInput_GetTotalInputCount_ReturnsCorrectCount()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.Right };
            motion.buttons = new List<InputButton> { InputButton.Light, InputButton.Medium };
            
            Assert.AreEqual(4, motion.GetTotalInputCount());
        }
    }
    
    public class MotionRecognizerTests
    {
        [Test]
        public void MotionRecognizer_CheckSequence_ReturnsTrueForValidDirectionSequence()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.Right };
            motion.allowDirtyInputs = false;
            motion.maxFrameWindow = 30;
            
            var inputHistory = new List<InputCommand>
            {
                InputCommand.CreateDirectionInput(InputDirection.Down, Vector2.down),
                InputCommand.CreateDirectionInput(InputDirection.Right, Vector2.right)
            };
            
            // Set frame numbers manually for test
            inputHistory[0] = new InputCommand(inputHistory[0].type, inputHistory[0].direction, inputHistory[0].inputDirection, inputHistory[0].button, inputHistory[0].isPressed) { frame = 1 };
            inputHistory[1] = new InputCommand(inputHistory[1].type, inputHistory[1].direction, inputHistory[1].inputDirection, inputHistory[1].button, inputHistory[1].isPressed) { frame = 5 };
            
            bool result = MotionRecognizer.CheckSequence(inputHistory, motion);
            Assert.IsTrue(result);
        }
        
        [Test]
        public void MotionRecognizer_CheckSequence_ReturnsTrueForValidButtonSequence()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.buttons = new List<InputButton> { InputButton.Light };
            motion.allowDirtyInputs = false;
            motion.maxFrameWindow = 30;
            
            var inputHistory = new List<InputCommand>
            {
                InputCommand.CreateButtonInput(InputButton.Light, true)
            };
            
            bool result = MotionRecognizer.CheckSequence(inputHistory, motion);
            Assert.IsTrue(result);
        }
        
        [Test]
        public void MotionRecognizer_CheckSequence_ReturnsFalseForInvalidSequence()
        {
            var motion = ScriptableObject.CreateInstance<MotionInput>();
            motion.directions = new List<InputDirection> { InputDirection.Down, InputDirection.Right };
            motion.allowDirtyInputs = false;
            motion.maxFrameWindow = 30;
            
            var inputHistory = new List<InputCommand>
            {
                InputCommand.CreateDirectionInput(InputDirection.Up, Vector2.up),
                InputCommand.CreateDirectionInput(InputDirection.Left, Vector2.left)
            };
            
            bool result = MotionRecognizer.CheckSequence(inputHistory, motion);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void MotionRecognizer_CreateQuarterCircleForward_CreatesValidMotion()
        {
            var motion = MotionRecognizer.CreateQuarterCircleForward(InputButton.Light);
            
            Assert.AreEqual(3, motion.directions.Count);
            Assert.AreEqual(InputDirection.Down, motion.directions[0]);
            Assert.AreEqual(InputDirection.DownRight, motion.directions[1]);
            Assert.AreEqual(InputDirection.Right, motion.directions[2]);
            Assert.AreEqual(1, motion.buttons.Count);
            Assert.AreEqual(InputButton.Light, motion.buttons[0]);
        }
    }
    
    public class InputBufferTests
    {
        private GameObject testObject;
        private InputBuffer inputBuffer;
        
        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestInputBuffer");
            inputBuffer = testObject.AddComponent<InputBuffer>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void InputBuffer_AddInput_IncreasesInputCount()
        {
            var command = InputCommand.CreateButtonInput(InputButton.Light);
            
            Assert.AreEqual(0, inputBuffer.CurrentInputCount);
            
            inputBuffer.AddInput(command);
            
            Assert.AreEqual(1, inputBuffer.CurrentInputCount);
        }
        
        [Test]
        public void InputBuffer_AddDirectionInput_AddsCorrectCommand()
        {
            inputBuffer.AddDirectionInput(InputDirection.Right, Vector2.right);
            
            Assert.AreEqual(1, inputBuffer.CurrentInputCount);
            
            var recent = inputBuffer.GetMostRecentInput();
            Assert.IsTrue(recent.HasValue);
            Assert.AreEqual(InputType.Direction, recent.Value.type);
            Assert.AreEqual(InputDirection.Right, recent.Value.inputDirection);
        }
        
        [Test]
        public void InputBuffer_AddButtonInput_AddsCorrectCommand()
        {
            inputBuffer.AddButtonInput(InputButton.Heavy, true);
            
            Assert.AreEqual(1, inputBuffer.CurrentInputCount);
            
            var recent = inputBuffer.GetMostRecentInput();
            Assert.IsTrue(recent.HasValue);
            Assert.AreEqual(InputType.Button, recent.Value.type);
            Assert.AreEqual(InputButton.Heavy, recent.Value.button);
            Assert.IsTrue(recent.Value.isPressed);
        }
        
        [Test]
        public void InputBuffer_ClearBuffer_RemovesAllInputs()
        {
            inputBuffer.AddButtonInput(InputButton.Light);
            inputBuffer.AddButtonInput(InputButton.Medium);
            
            Assert.AreEqual(2, inputBuffer.CurrentInputCount);
            
            inputBuffer.ClearBuffer();
            
            Assert.AreEqual(0, inputBuffer.CurrentInputCount);
        }
    }
}