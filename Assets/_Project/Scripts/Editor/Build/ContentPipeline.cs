using Game.Editor.Boss;
using Game.Editor.Enemies;
using Game.Editor.Level;
using Game.Editor.Sandbox;
using Game.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Build
{
    /// <summary>
    /// Koddan üretilen bütün içeriği sırayla kurar: oyuncu canı, arayüz ve sahne akışı, düşmanlar, boss, Level_01.
    /// Sıra önemlidir: bölüm kurucu prefab'ları, prefab'lar olay asset'lerini kullanır. Var olan ayar değerleri ezilmez.
    /// Batchmode: Unity.exe -batchmode -quit -projectPath . -executeMethod Game.Editor.Build.ContentPipeline.BuildAllContent
    /// </summary>
    public static class ContentPipeline
    {
        [MenuItem("Tools/Kanca/Tum Icerigi Kur")]
        public static void BuildAllContent()
        {
            Step("Oyuncu canı", PlayerHealthSetup.Apply);
            Step("Arayüz ve sahne akışı", UiFactory.BuildAll);
            Step("Düşmanlar", EnemyFactory.BuildAll);
            Step("Boss", BossFactory.BuildAll);
            Step("Level_01", LevelBuilder.BuildLevel01);
            AssetDatabase.SaveAssets();
            Debug.Log("[ContentPipeline] Tüm içerik kuruldu.");
        }

        static void Step(string label, System.Action action)
        {
            Debug.Log("[ContentPipeline] " + label + "...");
            action();
            AssetDatabase.SaveAssets();
        }
    }
}
