using System;

namespace Orbital2.Engine;

public class FixedList<T>
{
    public T[] Array { get; private set; }
    public int Used { get; private set; }
    
    public FixedList(int capacity = 0)
    {
        Array = new T[capacity];
    }

    public void Reset()
    {
        Used = 0;
    }
    
    public void Resize(int newSize)
    {
        if (Array.Length >= newSize) return;
        
        Array = new T[newSize];
    }
    
    public void Add(T item)
    {
        if (Used >= Array.Length)
        {
            throw new InvalidOperationException("List is full");
        }
        
        Array[Used++] = item;
    }
}