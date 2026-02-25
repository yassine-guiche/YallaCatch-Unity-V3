namespace YallaCatch.Editor
{
    /// <summary>
    /// GameplayMap scene generator (direct builder).
    /// </summary>
    internal static class GameplayMapSceneGenerator
    {
        internal const string ScenePath = "Assets/Scenes/GameplayMapScene.unity";

        internal static string GenerateGameplayMapSceneCompat(bool promptToSaveModifiedScenes = true)
        {
            return GeneratedGameplaySceneBuilder.Generate(
                GeneratedGameplaySceneBuilder.Variant.Map,
                ScenePath,
                promptToSaveModifiedScenes);
        }
    }
}
