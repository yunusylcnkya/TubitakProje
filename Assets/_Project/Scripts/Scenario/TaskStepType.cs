// IMPORTANT: Do not reorder existing values; old ScenarioDefinition assets
// serialize TaskStepType as an integer.
public enum TaskStepType
{
    EquipmentPlaced = 0,
    ConnectionCreated = 1,
    CircuitValid = 2,
    FunctionalState = 3
}

// 0 is the default of old serialized steps: they remain unchanged.
public enum ExpectedSwitchState
{
    Ignore = 0,
    On = 1,
    Off = 2
}
