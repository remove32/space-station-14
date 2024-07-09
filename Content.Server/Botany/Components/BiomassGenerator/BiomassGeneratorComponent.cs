using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Botany.Components.BiomassGenerator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class BiomassGeneratorComponent : Component
{
    /// <summary>
    /// This gets set for each plant it processes.
    /// When it hits 0, add biomass to buffer.
    /// </summary>
    [ViewVariables]
    public float ProcessingTimer = default;

    /// <summary>
    /// Amount of biomass that the plant being processed will yield.
    /// This is calculated from the YieldPerUnitMass.
    /// Also stores non-integer leftovers.
    /// </summary>
    [ViewVariables]
    public float CurrentExpectedYield = 0f;

    /// <summary>
    /// How many seconds to take to insert an entity per unit of its mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseInsertionDelay = 0.1f;

    /// <summary>
    /// How many units of biomass it produces for each unit of mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float YieldPerUnitMass = 0.6f;

    /// <summary>
    /// How much to multiply biomass yield from botany produce.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProduceYieldMultiplier = 1f;

    /// <summary>
    /// The time it takes to process a mob, per mass.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProcessingTimePerUnitMass = 0.5f;

    [ViewVariables]
    public float GeneratorProgress = 0;

    /// <summary>
    /// The material that is used to generate entities.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Biomass";

    /// <summary>
    /// The current amount of time it takes to generate a material
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GeneratingTime = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public BiomassGeneratorStatus Status;
}

//[Serializable, NetSerializable]
[Serializable]
public enum BiomassGeneratorVisuals : byte
{
    Status,
}

//[Serializable, NetSerializable]
[Serializable]
public enum BiomassGeneratorStatus : byte
{
    Idle,
    Generating,
}

[ByRefEvent]
public struct BiomassProductGeneratedEvent(EntityUid source, EntityUid target)
{
    public readonly EntityUid Source = source;
    public readonly EntityUid Target = target;
}
