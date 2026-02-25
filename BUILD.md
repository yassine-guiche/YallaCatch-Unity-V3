# YallaCatch v3.0 Unity Build Guide

## 🛠️ Requirements
- Unity 2022.3+
- TextMeshPro Essentials
- Google Mobile Ads SDK (AdMob)

## 🏗️ Generating the Scene
The entire UI is generated programmatically to maintain "Premium Rich" design consistency.
1. Open the Unity project.
2. Go to `YallaCatch` > `Generate Full Scene` in the top menu.
3. This will rebuild all panels, wire managers via reflection, and apply the glassmorphism theme.

## 📡 Configuration
- Edit `APIClient.cs` to update the `baseUrl` for Production vs Staging.
- Ensure `AdMobManager` has valid Ad Unit IDs for Android/iOS releases.

## 🎨 Asset Maintenance
- Most colors and styles are defined in `FullSceneGenerator.cs` helpers.
- To update the theme, modify the `Color` constants in `CreatePanel`, `CreateButton`, and `CreateListItemPrefab`.
