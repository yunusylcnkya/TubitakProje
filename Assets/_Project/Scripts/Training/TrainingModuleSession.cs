public static class TrainingModuleSession
{
    public static TrainingModuleDefinition
        SelectedModule
    {
        get;
        private set;
    }

    public static bool HasSelectedModule =>
        SelectedModule != null;

    public static void SelectModule(
        TrainingModuleDefinition module)
    {
        SelectedModule =
            module;
    }

    public static void Clear()
    {
        SelectedModule =
            null;
    }
}