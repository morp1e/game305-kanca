using System.Collections;
using System.Reflection;
using Game.Combat;
using Game.Core;
using Game.Input;
using Game.Level;
using Game.Player;
using Game.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class PlayerHealthPhysicsTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject player;
        PlayerMotor motor;
        PlayerHealth health;
        RespawnController respawn;
        PlayerHealthTuning tuning;
        HealthEvent healthEvent;
        GameEvent deathEvent;
        GameObject visual;
        GameObject floor;
        SpriteAnimSet animationSet;
        Sprite deathSprite;
        Sprite idleSprite;
        PlayerAnimDriver animDriver;

        static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

        void BuildPlayer(Vector2 position)
        {
            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = position;
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            respawn = player.AddComponent<RespawnController>();
            health = player.AddComponent<PlayerHealth>();
            visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            var animPlayer = visual.AddComponent<SpriteAnimPlayer>();
            animDriver = visual.AddComponent<PlayerAnimDriver>();
            SetField(animDriver, "animPlayer", animPlayer);
            var deathTexture = new Texture2D(1, 1);
            var idleTexture = new Texture2D(1, 1);
            deathSprite = Sprite.Create(deathTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            idleSprite = Sprite.Create(idleTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            animationSet = ScriptableObject.CreateInstance<SpriteAnimSet>();
            animationSet.clips = new[]
            {
                new SpriteAnimClip { name = "death", frames = new[] { deathSprite, deathSprite }, fps = 4f, loop = false },
                new SpriteAnimClip { name = "idle", frames = new[] { idleSprite }, fps = 8f, loop = true }
            };
            SetField(animPlayer, "spriteRenderer", spriteRenderer);
            SetField(animPlayer, "set", animationSet);
            tuning = ScriptableObject.CreateInstance<PlayerHealthTuning>();
            tuning.damageHitstop = 0f;
            tuning.invulnerabilitySeconds = .16f;
            tuning.hazardReturnDelay = .04f;
            healthEvent = ScriptableObject.CreateInstance<HealthEvent>();
            deathEvent = ScriptableObject.CreateInstance<GameEvent>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            SetField(motor, "input", input);
            SetField(respawn, "motor", motor);
            SetField(respawn, "deathEvent", deathEvent);
            SetField(respawn, "respawnDelay", .06f);
            SetField(health, "tuning", tuning);
            SetField(health, "healthEvent", healthEvent);
            SetField(health, "visual", visual);
            player.SetActive(true);
        }

        [TearDown]
        public void Clean()
        {
            if (player != null) Object.Destroy(player);
            if (tuning != null) Object.Destroy(tuning);
            if (healthEvent != null) Object.Destroy(healthEvent);
            if (deathEvent != null) Object.Destroy(deathEvent);
            if (floor != null) Object.Destroy(floor);
            if (animationSet != null) Object.Destroy(animationSet);
            if (deathSprite != null) { Object.Destroy(deathSprite.texture); Object.Destroy(deathSprite); }
            if (idleSprite != null) { Object.Destroy(idleSprite.texture); Object.Destroy(idleSprite); }
        }

        [UnityTest]
        public IEnumerator ContactDamageHonorsInvulnerabilityAndCanHitAgainAfterWindow()
        {
            BuildPlayer(Vector2.zero);
            // Geri itme ve yerçekimi oyuncuyu küçük bir kutudan çıkarır; temasın sürmesi için kutu geniş, itme sıfır.
            tuning.knockbackSpeed = Vector2.zero;
            var enemy = new GameObject("Enemy") { layer = LayerMask.NameToLayer("Enemy") };
            var area = enemy.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(40f, 40f);
            enemy.AddComponent<ContactDamage>();
            yield return new WaitForFixedUpdate();
            Assert.That(health.CurrentHealth, Is.EqualTo(4f), "ilk temas 1 can düşürmeli");
            yield return new WaitForFixedUpdate();
            Assert.That(health.CurrentHealth, Is.EqualTo(4f), "dokunulmazlıkta sürekli temas hasar vermemeli");
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.That(health.CurrentHealth, Is.EqualTo(3f), "dokunulmazlık bitince Stay yeniden hasar vermeli");
            Object.Destroy(enemy);
        }

        [UnityTest]
        public IEnumerator LethalHitRaisesDeathAndRespawnRefillsHealthEvent()
        {
            BuildPlayer(Vector2.zero);
            int deaths = 0;
            deathEvent.Register(() => deaths++);
            for (int i = 0; i < 4; i++)
            {
                health.TakeHit(new HitInfo(1f, Vector2.right, 0f, player));
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
            }
            Assert.That(health.CurrentHealth, Is.EqualTo(1f), "dokunulmazlık aralığından sonra gelen dört vuruş 4 can düşürmeli");
            // Ölüm animasyonu bitmeden respawn gecikmesi başlamaz.
            health.TakeHit(new HitInfo(1f, Vector2.right, 0f, player));
            Assert.That(respawn.IsDeathAnimationPending, Is.True);
            Assert.That(respawn.IsDead, Is.False);
            for (int step = 0; step < 40 && !respawn.IsDead; step++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.True);
            Assert.That(deaths, Is.EqualTo(1));
            health.TakeHit(new HitInfo(1f, Vector2.right, 0f, player));
            Assert.That(deaths, Is.EqualTo(1), "ölüyken gelen vuruş ikinci ölüm saymamalı");
            for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
            Assert.That(health.CurrentHealth, Is.EqualTo(5f));
            Assert.That(healthEvent.Current, Is.EqualTo(5f));
            Assert.That(healthEvent.Max, Is.EqualTo(5f));
        }

        [UnityTest]
        public IEnumerator LethalHitPlaysDeathBeforeRespawnAndReturnsToIdle()
        {
            BuildPlayer(Vector2.zero);
            floor = new GameObject("Ground") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector2(0f, -0.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(8f, 1f);
            SetField(respawn, "respawnDelay", 0.25f);
            health.TakeHit(new HitInfo(5f, Vector2.right, 0f, player));

            Assert.That(respawn.IsDeathAnimationPending, Is.True);
            Assert.That(respawn.IsDead, Is.False);
            Assert.That(visual.activeSelf, Is.True, "death klibi oynarken Visual açık kalmalı");
            Assert.That(motor.ExternalControl, Is.True, "ölüm klibi boyunca oyuncu kontrolü kilitli olmalı");
            Assert.That(animDriver.ActiveClipName, Is.EqualTo("death"));

            yield return new WaitForSeconds(.15f);
            Assert.That(respawn.IsDeathAnimationPending, Is.True);
            Assert.That(visual.activeSelf, Is.True);
            yield return new WaitForSeconds(.42f);
            Assert.That(respawn.IsDead, Is.True, "respawn sayacı ölüm klibinden sonra başlamalı");
            Assert.That(visual.activeSelf, Is.False);
            for (int i = 0; i < 20 && respawn.IsDead; i++) yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(respawn.IsDead, Is.False);
            Assert.That(visual.activeSelf, Is.True);
            Assert.That(motor.ExternalControl, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(5f));
            Assert.That(animDriver.ActiveClipName, Is.EqualTo("idle"));
        }

        [UnityTest]
        public IEnumerator DamageAndReturnMovesPlayerToHazardZoneWithoutDeath()
        {
            BuildPlayer(new Vector2(-3f, 2f));
            var zone = new GameObject("SafeZone");
            zone.transform.position = new Vector2(-3f, 2f);
            zone.AddComponent<BoxCollider2D>().size = new Vector2(4f, 4f);
            zone.AddComponent<HazardRespawnZone>();
            // Dönüş noktasının altında zemin: oyuncu ışınlandıktan sonra düşmeden orada kalsın.
            var floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector2(-3f, 1.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
            var spikes = new GameObject("Spikes") { layer = LayerMask.NameToLayer("Hazard") };
            spikes.transform.position = new Vector2(0f, 2f);
            spikes.AddComponent<BoxCollider2D>().isTrigger = true;
            spikes.AddComponent<Hazard>();
            yield return new WaitForFixedUpdate();
            player.transform.position = new Vector2(0f, 2f);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.That(health.CurrentHealth, Is.EqualTo(4f));
            Assert.That(respawn.IsDead, Is.False);
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            // Dönüş noktası bölge merkezi + spawnOffset (0, 1): (-3, 3). Oyuncu dikenin üstünde (x = 0) kalmamalı.
            Assert.That(motor.Position.x, Is.EqualTo(-3f).Within(.01f));
            Assert.That(motor.Position.y, Is.InRange(2.8f, 3.05f), "oyuncu dönüş noktasında, zeminin üstünde durmalı");
            Assert.That(healthEvent.Current, Is.EqualTo(4f));
            Object.Destroy(zone);
            Object.Destroy(floor);
            Object.Destroy(spikes);
        }

        [UnityTest]
        public IEnumerator InstantDeathHazardKillsPlayer()
        {
            BuildPlayer(Vector2.zero);
            tuning.hazardPolicy = HazardPolicy.InstantDeath;
            var spikes = new GameObject("Spikes") { layer = LayerMask.NameToLayer("Hazard") };
            spikes.AddComponent<BoxCollider2D>().isTrigger = true;
            spikes.AddComponent<Hazard>();
            for (int i = 0; i < 40 && !respawn.IsDead; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.IsDead, Is.True);
            Object.Destroy(spikes);
        }
    }
}
