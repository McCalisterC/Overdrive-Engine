using System;
using System.Collections.Generic;

namespace FightingFramework.Utilities
{
    [System.Serializable]
    public class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int head;
        private int tail;
        private int count;
        private readonly int capacity;
        
        public int Count => count;
        public int Capacity => capacity;
        public bool IsFull => count == capacity;
        public bool IsEmpty => count == 0;
        
        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));
                
            this.capacity = capacity;
            buffer = new T[capacity];
            head = 0;
            tail = 0;
            count = 0;
        }
        
        public void Add(T item)
        {
            buffer[head] = item;
            head = (head + 1) % capacity;
            
            if (IsFull)
            {
                tail = (tail + 1) % capacity;
            }
            else
            {
                count++;
            }
        }
        
        public T GetMostRecent()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Buffer is empty");
                
            int mostRecentIndex = head == 0 ? capacity - 1 : head - 1;
            return buffer[mostRecentIndex];
        }
        
        public T GetOldest()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Buffer is empty");
                
            return buffer[tail];
        }
        
        public List<T> GetRecentItems(int itemCount)
        {
            if (itemCount <= 0)
                return new List<T>();
                
            itemCount = Math.Min(itemCount, count);
            var result = new List<T>(itemCount);
            
            for (int i = 0; i < itemCount; i++)
            {
                int index = (head - 1 - i + capacity) % capacity;
                result.Add(buffer[index]);
            }
            
            return result;
        }
        
        public List<T> GetAllItems()
        {
            var result = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                int index = (tail + i) % capacity;
                result.Add(buffer[index]);
            }
            
            return result;
        }
        
        public void Clear()
        {
            head = 0;
            tail = 0;
            count = 0;
            Array.Clear(buffer, 0, capacity);
        }
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                    
                int actualIndex = (tail + index) % capacity;
                return buffer[actualIndex];
            }
        }
    }
}