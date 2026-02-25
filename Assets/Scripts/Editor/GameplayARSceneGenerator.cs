namespace YallaCatch.Editor
{
    /// <summary>
    /// GameplayAR scene generator (direct builder).
    /// </summary>
    internal static class GameplayARSceneGenerator
    {
        internal const string ScenePath = "Assets/Scenes/GameplayARScene.unity";

        internal static string GenerateGameplayARSceneCompat(bool promptToSaveModifiedScenes = true)
        {
            return GeneratedGameplaySceneBuilder.Generate(
                GeneratedGameplaySceneBuilder.Variant.AR,
                ScenePath,
                promptToSaveModifiedScenes);
        }
    }
}
