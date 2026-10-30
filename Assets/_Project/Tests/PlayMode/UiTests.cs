using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Flow;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests
{
    public class UiTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<Object> created = new List<Object>();

        static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

        T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        [TearDown]
        public void Clean()
        {
            Time.timeScale = 1f;
            foreach (var obj in created)
                if (obj != null) Object.Destroy(obj);
            created.Clear();
        }

        HealthHud BuildHealthHud(HealthEvent channel)
        {
            var root = Track(new GameObject("Hud", typeof(RectTransform)));
            root.SetActive(false);
            var template = new GameObject("Template", typeof(RectTransform)).AddComponent<Image>();
            template.transform.SetParent(root.transform, false);
            var hud = root.AddComponent<HealthHud>();
            SetField(hud, "health", channel);
            SetField(hud, "container", (RectTransform)root.transform);
            SetField(hud, "iconTemplate", template);
            root.SetActive(true);
            return hud;
        }

        [UnityTest]
        public IEnumerator HealthHud_ShowsMaxIconsAndFilledCount_AndLateHudReadsLastValue()
        {
            var channel = Track(ScriptableObject.CreateInstance<HealthEvent>());
            channel.Raise(3f, 5f);
            // Kanal yayın yaptıktan sonra kurulan HUD son değeri hemen göstermeli.
            var hud = BuildHealthHud(channel);
            yield return null;
            Assert.That(hud.IconCount, Is.EqualTo(5));
            Assert.That(hud.FilledCount, Is.EqualTo(3));
            channel.Raise(2f, 5f);
            Assert.That(hud.FilledCount, Is.EqualTo(2));
            channel.Raise(2f, 6f);
            Assert.That(hud.IconCount, Is.EqualTo(6), "en yüksek can artınca ikon eklenmeli");
        }

        [UnityTest]
        public IEnumerator BossBar_HiddenUntilStarted_FillTracksHealth_TrailLagsBehind_HidesOnRespawn()
        {
            var started = Track(ScriptableObject.CreateInstance<StringEvent>());
            var health = Track(ScriptableObject.CreateInstance<HealthEvent>());
            var respawned = Track(ScriptableObject.CreateInstance<GameEvent>());
            var root = Track(new GameObject("Boss", typeof(RectTransform)));
            root.SetActive(false);
            var group = root.AddComponent<CanvasGroup>();
            var fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(root.transform, false);
            var trail = new GameObject("Trail", typeof(RectTransform)).GetComponent<RectTransform>();
            trail.SetParent(root.transform, false);
            var bar = root.AddComponent<BossHealthBar>();
            SetField(bar, "fightStarted", started);
            SetField(bar, "health", health);
            SetField(bar, "playerRespawned", respawned);
            SetField(bar, "group", group);
            SetField(bar, "fill", fill);
            SetField(bar, "trail", trail);
            SetField(bar, "fadeDuration", 0f);
            root.SetActive(true);

            Assert.That(group.alpha, Is.EqualTo(0f), "dövüş başlamadan çubuk görünmemeli");
            started.Raise("Zincirli Bekçi");
            health.Raise(24f, 24f);
            yield return null;
            Assert.That(group.alpha, Is.EqualTo(1f));
            health.Raise(12f, 24f);
            Assert.That(bar.FillRatio, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(bar.TrailRatio, Is.EqualTo(1f).Within(1e-4f), "hasar izi hemen inmemeli");
            respawned.Raise();
            yield return null;
            Assert.That(bar.IsVisible, Is.False, "oyuncu yeniden doğunca dövüş sıfırlanır, çubuk gizlenmeli");
        }

        [UnityTest]
        public IEnumerator PauseMenu_StopsTime_RestoresOnResume_AndOnDestroy()
        {
            var root = Track(new GameObject("Pause"));
            var menu = root.AddComponent<PauseMenu>();
            yield return null;
            menu.Pause();
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            menu.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            menu.Pause();
            Object.Destroy(root);
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f), "menü açıkken yok edilirse zaman geri gelmeli");
        }

        [UnityTest]
        public IEnumerator RunStats_CountsDeathsOnlyWhileRunning()
        {
            var death = Track(ScriptableObject.CreateInstance<GameEvent>());
            var root = Track(new GameObject("Stats"));
            root.SetActive(false);
            var stats = root.AddComponent<RunStats>();
            SetField(stats, "playerDeath", death);
            root.SetActive(true);
            yield return null;
            death.Raise();
            Assert.That(stats.Deaths, Is.EqualTo(0), "oyun başlamadan ölüm sayılmamalı");
            stats.Begin();
            death.Raise();
            death.Raise();
            Assert.That(stats.Deaths, Is.EqualTo(2));
            stats.Stop();
            death.Raise();
            Assert.That(stats.Deaths, Is.EqualTo(2));
        }
    }
}
