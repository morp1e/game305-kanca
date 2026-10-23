#if UNITY_EDITOR
using System.Collections;
using System.IO;
using Game.Hook;
using Game.Level;
using Game.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // Kurucunun ürettiği gerçek deneme sahnesini açar ve oyunun ayağa kalktığını doğrular.
    // Sahne yoksa (kurucu hiç çalıştırılmamışsa) test atlanır.
    public class SandboxSceneSmokeTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Hook.unity";

        // Sahne Single modda açılır ve kalırsa sonraki testler onun duvarlarının içinde koşar.
        [UnityTearDown]
        public IEnumerator UnloadSandbox()
        {
            var sandbox = SceneManager.GetSceneByPath(ScenePath);
            if (!sandbox.IsValid() || !sandbox.isLoaded) yield break;
            SceneManager.SetActiveScene(SceneManager.CreateScene("AfterSmoke"));
            yield return SceneManager.UnloadSceneAsync(sandbox);
        }

        [UnityTest]
        public IEnumerator SandboxScene_PlayerStandsAndRespawns()
        {
            if (!File.Exists(ScenePath)) Assert.Ignore("Sandbox_Hook sahnesi yok; önce Tools/Kanca/Gri Kutu Sahnesini Kur.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();

            var motor = Object.FindAnyObjectByType<PlayerMotor>();
            Assert.That(motor, Is.Not.Null, "sahnede PlayerMotor yok");
            Assert.That(motor.IsGrounded, Is.True, $"oyuncu zeminde değil, y={motor.Position.y:F2}");
            Assert.That(Object.FindAnyObjectByType<GrappleHook>().State, Is.EqualTo(HookState.InHand));
            Assert.That(Object.FindAnyObjectByType<NonHookable>(), Is.Not.Null, "NonHookable deneme engeli yok");

            var respawn = motor.GetComponent<RespawnController>();
            Assert.That(respawn, Is.Not.Null, "oyuncuda RespawnController yok");
            Vector2 start = motor.Position;
            motor.Velocity = new Vector2(8f, 0f);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            respawn.Kill();
            Assert.That(respawn.IsDead, Is.True);
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.False, "yeniden doğmadı");
            Assert.That(Vector2.Distance(motor.Position, respawn.CurrentCheckpoint), Is.LessThan(1f), "checkpoint'te doğmadı");
        }
    }
}
#endif
