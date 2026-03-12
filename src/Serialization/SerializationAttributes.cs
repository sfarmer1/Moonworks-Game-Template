using System;

namespace Tactician.Serialization;

/// <summary>
/// Marks a component as serializable with a stable type identifier.
/// Used for components with data fields.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class SerializableComponentAttribute : Attribute
{
    /// <summary>
    /// Stable string identifier for this component type.
    /// Used to identify component types across save/load sessions.
    /// </summary>
    public required string ComponentTypeId { get; init; }
}

/// <summary>
/// Marks an empty marker component as serializable with a stable type identifier.
/// Used for components without data fields.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class SerializableMarkerAttribute : Attribute
{
    /// <summary>
    /// Stable string identifier for this marker component type.
    /// Used to identify component types across save/load sessions.
    /// </summary>
    public required string ComponentTypeId { get; init; }
}

/// <summary>
/// Explicitly marks a component as transient (not serialized).
/// Transient components are derived from other state and recreated on load.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public class TransientComponentAttribute : Attribute
{
}
