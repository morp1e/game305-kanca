using System.Collections;
using System.Reflection;
using Game.Core;
using Game.Input;
using Game.Level;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class LevelPhysicsTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject player;
        PlayerMotor motor;
        RespawnController respawn;
        PlayerMovementTuning tuning;
        GameEvent deathEvent;
        GameEvent respawnEvent;

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        void BuildPlayer(Vector2 position, float delay = 0.08f)
        {
            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = position;
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            respawn = player.AddComponent<RespawnController>();
            tuning = ScriptableObject.CreateInstance<PlayerMovementTuning>();
            deathEvent = ScriptableObject.CreateInstance<GameEvent>();
            respawnEvent = ScriptableObject.CreateInstance<GameEvent>();
            SetField(motor, "tuning", tuning);
            SetField(motor, "input", input);
            SetField(respawn, "motor", motor);
            SetField(respawn, "deathEvent", deathEvent);
            SetField(respawn, "respawnEvent", respawnEvent);
            SetField(respawn, "respawnDelay", delay);
            player.SetActive(true);
        }

        [TearDown]
        public void Clean()
        {
            if (player != null) Object.Destroy(player);
            if (tuning != null) Object.Destroy(tuning);
            if (deathEvent != null) Object.Destroy(deathEvent);
            if (respawnEvent != null) Object.Destroy(respawnEvent);
        }

        [UnityTest]
        public IEnumerator HazardKillsAndRespawnsAtCheckpoint()
        {
            BuildPlayer(new Vector2(0f, 3f));
            var spikes = new GameObject("Spikes") { layer = LayerMask.NameToLayer("Hazard") };
            spikes.transform.position = new Vector2(0f, 1.5f);
            spikes.AddComponent<BoxCollider2D>().size = new Vector2(4f, 0.3f);
            spikes.AddComponent<Hazard>();

            int deaths = 0, respawns = 0;
            deathEvent.Register(() => deaths++);
            respawnEvent.Register(() => respawns++);
            respawn.SetCheckpoint(new Vector2(5f, 4f));
            motor.Velocity = Vector2.down * 8f;

            for (int i = 0; i < 60 && !respawn.IsDead; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.True, "diken ölüm tetiklemedi");
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(motor.ExternalControl, Is.True);
            for (int i = 0; i < 30 && respawn.IsDead; i++) yield return new WaitForFixedUpdate();

            Assert.That(respawn.IsDead, Is.False);
            Assert.That(respawns, Is.EqualTo(1));
            Assert.That(motor.Position, Is.EqualTo(new Vector2(5f, 4f)));
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(motor.ExternalControl, Is.False);
            Object.Destroy(spikes);
        }

        [UnityTest]
        public IEnumerator CrossingCheckpointChangesRespawnPosition()
        {
            BuildPlayer(new Vector2(-3f, 3f));
            var checkpointObject = new GameObject("Checkpoint");
            checkpointObject.transform.position = new Vector2(0f, 3f);
            checkpointObject.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            var checkpoint = checkpointObject.AddComponent<Checkpoint>();
            SetField(checkpoint, "spawnOffset", new Vector2(0f, 2f));
            motor.ExternalControl = true;
            motor.Velocity = Vector2.right * 8f;

            for (int i = 0; i < 60 && respawn.CurrentCheckpoint.x < 0f; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.CurrentCheckpoint, Is.EqualTo(new Vector2(0f, 5f)));

            respawn.Kill();
            for (int i = 0; i < 30 && respawn.IsDead; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.False);
            Assert.That(motor.Position, Is.EqualTo(new Vector2(0f, 5f)));
            Object.Destroy(checkpointObject);
        }

        [UnityTest]
        public IEnumerator FallingBelowKillHeightKillsPlayer()
        {
            BuildPlayer(new Vector2(0f, 0f));
            SetField(respawn, "killHeight", -2f);
            motor.ExternalControl = true;
            motor.Velocity = Vector2.down * 10f;

            for (int i = 0; i < 60 && !respawn.IsDead; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.True, "killHeight altında ölüm tetiklenmedi");
        }
    }
}
