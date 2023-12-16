using System;

namespace MLE;

public struct Pair<T>
{
    public double X { get; set; }
    
    public T Y { get; set; }

    public Pair(double x, T y)
    {
        X = x;
        Y = y;
    }
}