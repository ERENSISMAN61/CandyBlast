using UnityEngine;

/// <summary>
/// Defines block color types
/// </summary>
public enum BlockType
{
    Blue = 0,
    Green = 1,
    Pink = 2,
    Purple = 3,
    Red = 4,
    Yellow = 5
}

/// <summary>
/// Defines block icon variants based on group size
/// </summary>
public enum IconVariant
{
    Default = 0,  // Less than A+1
    A = 1,        // More than A
    B = 2,        // More than B
    C = 3         // More than C
}
