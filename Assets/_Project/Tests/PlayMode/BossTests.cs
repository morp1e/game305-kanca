using System.Reflection;
using Game.Boss;
using Game.Combat;
using Game.Core;
using Game.Hook;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace Game.Tests
{
    public class BossTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject root;
        GameObject arenaRoot;
        BossDefinition definition;
        BossPhase phaseOne;
        BossPhase phaseTwo;
        HealthEvent health;
        GameEvent defeated;
        GameEvent won;
        Transform frontSource;
        Transform rearSource;
        BossShield shield;
        HookPullable shieldPullable;

        static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<BossDefinition>();
            phaseOne = ScriptableObject.CreateInstance<BossPhase>();
            phaseTwo = ScriptableObject.CreateInstance<BossPhase>();
            phaseOne.hasShield = true;
            phaseTwo.hasShield = false;
            phaseTwo.enterAtHealth = 10f;
            phaseTwo.transitionDuration = 0.05f;
            definition.phases = new[] { phaseOne, phaseTwo };
            definition.victoryDelay = 0.05f;
            definition.introDuration = 0.01f;
            definition.shieldStagger = 0.03f;
            definition.shieldRewind = 0.03f;
            health = ScriptableObject.CreateInstance<HealthEvent>();
            defeated = ScriptableObject.CreateInstance<GameEvent>();
            won = ScriptableObject.CreateInstance<GameEvent>();
            root = new GameObject("Warden");
            root.SetActive(false);
            root.layer = LayerMask.NameToLayer("Enemy");
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            root.AddComponent<BoxCollider2D>().size = new Vector2(3f, 3f);
            var renderer = root.AddComponent<SpriteRenderer>();
            var contact = root.AddComponent<ContactDamage>();
            var brain = root.AddComponent<BossBrain>();
            var shieldObject = new GameObject("Shield");
            shieldObject.transform.SetParent(root.transform, false);
            shieldObject.layer = LayerMask.NameToLayer("Enemy");
            var shieldBody = shieldObject.AddComponent<Rigidbody2D>();
            shieldBody.bodyType = RigidbodyType2D.Kinematic;
            shieldObject.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2.5f);
            shieldObject.AddComponent<NonHookable>();
            shieldPullable = shieldObject.AddComponent<HookPullable>();
            Set(shieldPullable, "weightClass", HookWeightClass.Light);
            Set(shieldPullable, "body", shieldBody);
            shield = shieldObject.AddComponent<BossShield>();
            Set(shield, "boss", brain);
            Set(shield, "body", shieldBody);
            Set(shield, "pullable", shieldPullable);
            Set(brain, "shield", shield);
            var front = new GameObject("FrontSource");
            front.transform.SetParent(root.transform, false);
            frontSource = front.transform;
            var rear = new GameObject("RearSource");
            rear.transform.SetParent(root.transform, false);
            rearSource = rear.transform;
            Set(brain, "definition", definition);
            Set(brain, "healthEvent", health);
            Set(brain, "defeatedEvent", defeated);
            Set(brain, "gameWonEvent", won);
            Set(brain, "visual", renderer);
            Set(brain, "contactDamage", contact);
            root.SetActive(true);
            brain.ResetFight();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (arenaRoot != null) Object.DestroyImmediate(arenaRoot);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(phaseOne);
            Object.DestroyImmediate(phaseTwo);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(defeated);
            Object.DestroyImmediate(won);
        }

        /// <summary>Uyuyan ve giriş yapan boss dokunulmazdır; testler önce dövüşü başlatır.</summary>
        static IEnumerator BeginAndWait(BossBrain brain)
        {
            brain.BeginFight();
            for (int i = 0; i < 200 && brain.IsInvulnerable; i++) yield return null;
            Assert.IsFalse(brain.IsInvulnerable, "giriş bitmedi");
        }

        [Test]
        public void DormantBossIgnoresHits()
        {
            var brain = root.GetComponent<BossBrain>();
            rearSource.position = new Vector3(-2f, 0f);
            brain.TakeHit(new HitInfo(1f, Vector2.left, 0f, rearSource));
            Assert.AreEqual(24f, brain.Health, "dövüş başlamadan boss hasar almamalı");
        }

        [UnityTest]
        public IEnumerator FrontShieldBlocksAndRearHitDamages()
        {
            var brain = root.GetComponent<BossBrain>();
            yield return BeginAndWait(brain);
            frontSource.position = new Vector3(2f, 0f);
            brain.TakeHit(new HitInfo(1f, Vector2.left, 0f, frontSource));
            Assert.AreEqual(24f, brain.Health);
            rearSource.position = new Vector3(-2f, 0f);
            brain.TakeHit(new HitInfo(1f, Vector2.left, 0f, rearSource));
            Assert.AreEqual(23f, brain.Health);
        }

        [UnityTest]
        public IEnumerator OnlyLooseShieldCanBeHookedAndLooseHitStaggersBoss()
        {
            var brain = root.GetComponent<BossBrain>();
            Assert.IsNotNull(shield.GetComponent<NonHookable>());
            shield.SetLoose(false);
            Assert.IsNotNull(shield.GetComponent<NonHookable>());
            shield.SetLoose(true);
            yield return null;
            Assert.IsNull(shield.GetComponent<NonHookable>());
            yield return BeginAndWait(brain);
            // Gerçek kanca sırası: takıl (Hooked) -> hedefi sar -> varışta sıfır hasarlı sersemletme vuruşu.
            shieldPullable.NotifyHooked();
            rearSource.position = new Vector3(-2f, 0f);
            brain.TakeHit(new HitInfo(0f, Vector2.zero, 0f, rearSource, 0.1f));
            Assert.AreEqual(BossState.Staggered, brain.State);
            Assert.IsFalse(shield.IsAttached);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(shield.IsAttached, "sendeleme ve geri sarma bitince kalkan yuvasında olmalı");
        }

        [UnityTest]
        public IEnumerator LowHealthWaitsForActiveAttackThenBreaksShield()
        {
            var brain = root.GetComponent<BossBrain>();
            var attack = ScriptableObject.CreateInstance<BossAttack>();
            attack.id = "test_attack";
            attack.telegraph = 0.01f;
            attack.active = 0.08f;
            attack.recovery = 0.08f;
            attack.hitboxes = System.Array.Empty<BossHitbox>();
            phaseOne.idleMin = 0f;
            phaseOne.idleMax = 0f;
            phaseOne.attacks = new[] { new BossAttackEntry { attack = attack, weight = 1f, maxRange = 20f } };
            shield.Break();
            brain.BeginFight();
            // Batchmode'da kare süresi çok kısa; kare saymak yerine süreyle bekle.
            float deadline = Time.time + 2f;
            while (brain.State != BossState.Active && Time.time < deadline) yield return null;
            Assert.AreEqual(BossState.Active, brain.State);
            frontSource.position = new Vector3(2f, 0f);
            for (int i = 0; i < 15; i++) brain.TakeHit(new HitInfo(1f, Vector2.left, 0f, frontSource));
            Assert.AreEqual(BossState.Active, brain.State);
            deadline = Time.time + 2f;
            while (brain.State != BossState.PhaseTransition && Time.time < deadline) yield return null;
            Assert.AreEqual(BossState.PhaseTransition, brain.State);
            Assert.IsTrue(shield.IsBroken);
            Object.DestroyImmediate(attack);
        }

        [Test]
        public void ArenaClosesDoorsOnEntryAndRespawnResetsBoss()
        {
            arenaRoot = new GameObject("Arena");
            var body = arenaRoot.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var arena = arenaRoot.AddComponent<BossArena>();
            var door = new GameObject("Door");
            door.transform.SetParent(arenaRoot.transform);
            door.SetActive(false);
            var started = ScriptableObject.CreateInstance<StringEvent>();
            Set(arena, "boss", root.GetComponent<BossBrain>());
            Set(arena, "definition", definition);
            Set(arena, "doors", new[] { door });
            Set(arena, "fightStarted", started);
            bool eventRaised = false;
            started.Register(_ => eventRaised = true);
            arena.StartFight();
            Assert.IsTrue(door.activeSelf);
            Assert.IsTrue(eventRaised);
            arena.ResetFight();
            Assert.IsFalse(door.activeSelf);
            Assert.AreEqual(BossState.Dormant, root.GetComponent<BossBrain>().State);
            Object.DestroyImmediate(started);
        }

        [UnityTest]
        public IEnumerator LethalHitRaisesDefeatAndDelayedWin()
        {
            var brain = root.GetComponent<BossBrain>();
            bool didDefeat = false;
            bool didWin = false;
            defeated.Register(() => didDefeat = true);
            won.Register(() => didWin = true);
            yield return BeginAndWait(brain);
            rearSource.position = new Vector3(-2f, 0f);
            for (int i = 0; i < 24; i++) brain.TakeHit(new HitInfo(1f, Vector2.left, 0f, rearSource));
            Assert.IsTrue(didDefeat);
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(didWin);
        }

    }
}
