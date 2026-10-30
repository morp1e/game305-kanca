using System.Collections;
using System.Reflection;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Hook;
using Game.Input;
using Game.Level;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Game.Tests
{
    public class EnemyPhysicsTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject enemy;
        EnemyDefinition definition;
        GameEvent respawnEvent;
        WalkerBrain brain;
        EnemyBrain activeBrain;
        EnemyHealth health;
        GameObject playerObject;
        PlayerHealth playerHealth;
        PlayerHealthTuning playerTuning;
        PlayerMovementTuning movementTuning;
        GameEvent playerRespawnEvent;
        ProjectileDefinition projectileDefinition;
        // Alan temel sınıfta private olabilir (EnemyBrain.definition); GetField türetilmiş türde onu görmez, zinciri yürü.
        static void SetField(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Private);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new System.MissingFieldException(target.GetType().Name, name);
        }
        void BuildWalker()
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.gravityScale = 0f;
            respawnEvent = ScriptableObject.CreateInstance<GameEvent>();
            enemy = new GameObject("Walker")  {
                layer = LayerMask.NameToLayer("Enemy")
            }
            ;
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<ContactDamage>();
            enemy.AddComponent<HookPullable>();
            brain = enemy.AddComponent<WalkerBrain>();
            activeBrain = brain;
            health = enemy.AddComponent<EnemyHealth>();
            SetField(brain, "definition", definition);
            SetField(health, "definition", definition);
            SetField(health, "brain", brain);
            SetField(health, "playerRespawnEvent", respawnEvent);
            enemy.SetActive(true);
        }

        void BuildPlayer()
        {
            playerObject = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            playerObject.SetActive(false);
            playerObject.transform.position = new Vector2(.25f, 0f);
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CapsuleCollider2D>().size = new Vector2(.8f, 1.8f);
            PlayerInputReader input = playerObject.AddComponent<PlayerInputReader>();
            PlayerMotor motor = playerObject.AddComponent<PlayerMotor>();
            RespawnController respawn = playerObject.AddComponent<RespawnController>();
            playerHealth = playerObject.AddComponent<PlayerHealth>();
            playerTuning = ScriptableObject.CreateInstance<PlayerHealthTuning>();
            playerTuning.damageHitstop = 0f;
            playerTuning.knockbackSpeed = Vector2.zero;
            // Gerçek dokunulmazlık: temas Stay'i her adımda tetikler, 0 olursa tek temas birden çok can götürür.
            playerTuning.invulnerabilitySeconds = 1f;
            movementTuning = ScriptableObject.CreateInstance<PlayerMovementTuning>();
            playerRespawnEvent = ScriptableObject.CreateInstance<GameEvent>();
            SetField(motor, "tuning", movementTuning);
            SetField(motor, "input", input);
            SetField(respawn, "motor", motor);
            SetField(playerHealth, "tuning", playerTuning);
            playerObject.SetActive(true);
        }
        [TearDown] public void Clean()
        {
            if (enemy != null) Object.Destroy(enemy);
            if (definition != null) Object.Destroy(definition);
            if (respawnEvent != null) Object.Destroy(respawnEvent);
            if (playerObject != null) Object.Destroy(playerObject);
            if (playerTuning != null) Object.Destroy(playerTuning);
            if (movementTuning != null) Object.Destroy(movementTuning);
            if (playerRespawnEvent != null) Object.Destroy(playerRespawnEvent);
            if (projectileDefinition != null) Object.Destroy(projectileDefinition);
            // Hedefini ıskalayan mermi 4 s yaşar; sonraki test sınıflarının oyuncusuna çarpmasın.
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Object.DestroyImmediate(projectile.gameObject);
        }
        [UnityTest] public IEnumerator HookedWalkerPreservesExternalVelocityAndSuppressesContact()
        {
            BuildWalker();
            enemy.GetComponent<HookPullable>().NotifyHooked();
            enemy.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(3f, 2f);
            yield return new WaitForFixedUpdate();
            Assert.That(enemy.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(new Vector2(3f, 2f)));
            Assert.That(enemy.GetComponent<ContactDamage>().Suppressed, Is.True);
        }
        [UnityTest] public IEnumerator ZeroDamageHookHitStunsAndLethalHitsRespawnWalker()
        {
            BuildWalker();
            health.TakeHit(new HitInfo(0f, Vector2.right, 0f, enemy.transform, .5f));
            Assert.That(GetState(), Is.EqualTo(EnemyState.Stunned));
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(GetState(), Is.EqualTo(EnemyState.Stunned));
            health.TakeHit(new HitInfo(1f, Vector2.right, 0f, enemy.transform));
            health.TakeHit(new HitInfo(1f, Vector2.right, 0f, enemy.transform));
            Assert.That(health.IsDead, Is.True);
            Assert.That(enemy.GetComponent<Collider2D>().enabled, Is.False);
            respawnEvent.Raise();
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.Health, Is.EqualTo(2f));
            Assert.That(enemy.GetComponent<Collider2D>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator WalkerTurnsAtGroundEdgeInsteadOfFalling()
        {
            GameObject floor = new GameObject("Ground") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector2(0f, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
            BuildWalker();
            enemy.transform.position = new Vector2(.7f, .5f);
            yield return new WaitForFixedUpdate();
            Assert.That(enemy.GetComponent<Rigidbody2D>().linearVelocity.x, Is.LessThan(0f));
            Assert.That(enemy.transform.position.y, Is.GreaterThanOrEqualTo(.45f));
            Object.Destroy(floor);
        }

        [UnityTest]
        public IEnumerator WalkerContactTakesOnePlayerHealth()
        {
            BuildPlayer();
            BuildWalker();
            enemy.transform.position = playerObject.transform.position;
            yield return new WaitForFixedUpdate();
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(4f));
        }

        [UnityTest]
        public IEnumerator BruteBlocksFrontDamageAndAcceptsRearDamage()
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.gravityScale = 0f;
            definition.shieldArcDegrees = 120f;
            respawnEvent = ScriptableObject.CreateInstance<GameEvent>();
            enemy = new GameObject("Brute") { layer = LayerMask.NameToLayer("Enemy") };
            enemy.SetActive(false);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<ContactDamage>();
            enemy.AddComponent<HookPullable>();
            BruteBrain brute = enemy.AddComponent<BruteBrain>();
            activeBrain = brute;
            health = enemy.AddComponent<EnemyHealth>();
            SetField(brute, "definition", definition);
            SetField(health, "definition", definition);
            SetField(health, "brain", brute);
            SetField(health, "playerRespawnEvent", respawnEvent);
            enemy.SetActive(true);
            GameObject attacker = new GameObject("Attacker");
            attacker.transform.position = Vector2.right;
            health.TakeHit(new HitInfo(1f, Vector2.left, 0f, attacker.transform));
            Assert.That(health.Health, Is.EqualTo(2f));
            attacker.transform.position = Vector2.left;
            health.TakeHit(new HitInfo(1f, Vector2.right, 0f, attacker.transform));
            Assert.That(health.Health, Is.EqualTo(1f));
            Object.Destroy(attacker);
            yield return null;
        }

        /// <summary>Zeminsiz test sahnesinde oyuncu yerçekimiyle saniyede ~10 karo düşer; hedef olarak sabit dursun.</summary>
        void HoldPlayerInPlace() => playerObject.GetComponent<PlayerMotor>().ExternalControl = true;

        [UnityTest]
        public IEnumerator FlyerTelegraphIsCancelledWhenHooked()
        {
            BuildPlayer();
            HoldPlayerInPlace();
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.gravityScale = 0f;
            definition.detectionRadius = 10f;
            definition.telegraphDuration = 1f;
            respawnEvent = ScriptableObject.CreateInstance<GameEvent>();
            enemy = new GameObject("Flyer") { layer = LayerMask.NameToLayer("Enemy") };
            enemy.SetActive(false);
            // Oyuncunun üstünde doğarsa telegraph'tan önce temas hasarı verir; testin konusu kancanın telegraph'ı kesmesi.
            enemy.transform.position = new Vector2(4f, 0f);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<BoxCollider2D>();
            enemy.AddComponent<ContactDamage>();
            HookPullable pullable = enemy.AddComponent<HookPullable>();
            FlyerBrain flyer = enemy.AddComponent<FlyerBrain>();
            activeBrain = flyer;
            health = enemy.AddComponent<EnemyHealth>();
            SetField(flyer, "definition", definition);
            SetField(flyer, "player", playerHealth);
            SetField(health, "definition", definition);
            SetField(health, "brain", flyer);
            SetField(health, "playerRespawnEvent", respawnEvent);
            enemy.SetActive(true);
            yield return new WaitForFixedUpdate();
            Assert.That(GetState(), Is.EqualTo(EnemyState.Telegraph));
            pullable.NotifyHooked();
            Assert.That(GetState(), Is.EqualTo(EnemyState.Pulled));
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(5f));
            Assert.That(GetState(), Is.EqualTo(EnemyState.Pulled));
        }

        [UnityTest]
        public IEnumerator EnemyProjectileDamagesPlayerAndDestroysItself()
        {
            BuildPlayer();
            HoldPlayerInPlace();
            projectileDefinition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            Projectile projectile = BuildProjectile(new Vector2(-1f, 0f), false);
            for (int i = 0; i < 20 && projectile != null; i++) yield return new WaitForFixedUpdate();
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(4f));
            Assert.That(projectile == null, Is.True);
        }

        [UnityTest]
        public IEnumerator ReflectedProjectileIgnoresPlayerAndDamagesEnemy()
        {
            BuildPlayer();
            projectileDefinition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            EnemyDefinition targetDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
            targetDefinition.maxHealth = 3f;
            GameObject target = new GameObject("Spitter") { layer = LayerMask.NameToLayer("Enemy") };
            target.SetActive(false);
            target.transform.position = new Vector2(2f, 0f);
            target.AddComponent<Rigidbody2D>().gravityScale = 0f;
            target.AddComponent<BoxCollider2D>();
            EnemyHealth targetHealth = target.AddComponent<EnemyHealth>();
            SetField(targetHealth, "definition", targetDefinition);
            target.SetActive(true);
            Projectile projectile = BuildProjectile(new Vector2(-1f, 0f), true);
            for (int i = 0; i < 30 && projectile != null; i++) yield return new WaitForFixedUpdate();
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(5f));
            Assert.That(targetHealth.Health, Is.EqualTo(2f));
            Assert.That(projectile == null, Is.True);
            Object.Destroy(target);
            Object.Destroy(targetDefinition);
        }


        [UnityTest]
        public IEnumerator SpitterFiresAtPlayerInSight()
        {
            BuildPlayer();
            HoldPlayerInPlace();
            playerObject.transform.position = new Vector2(4f, 0f);
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.gravityScale = 0f;
            definition.detectionRadius = 9f;
            definition.telegraphDuration = .04f;
            projectileDefinition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            projectileDefinition.speed = 6f;
            projectileDefinition.lifetime = 4f;
            GameObject projectileObject = new GameObject("ProjectilePrefab") { layer = LayerMask.NameToLayer("Enemy") };
            projectileObject.SetActive(false);
            projectileObject.AddComponent<Rigidbody2D>();
            projectileObject.AddComponent<CircleCollider2D>();
            Projectile projectilePrefab = projectileObject.AddComponent<Projectile>();
            projectileObject.SetActive(true);
            GameObject spitter = new GameObject("Spitter") { layer = LayerMask.NameToLayer("Enemy") };
            spitter.SetActive(false);
            spitter.AddComponent<Rigidbody2D>();
            spitter.AddComponent<BoxCollider2D>();
            spitter.AddComponent<ContactDamage>();
            spitter.AddComponent<HookPullable>();
            SpitterBrain spitterBrain = spitter.AddComponent<SpitterBrain>();
            EnemyHealth spitterHealth = spitter.AddComponent<EnemyHealth>();
            SetField(spitterBrain, "definition", definition);
            SetField(spitterBrain, "player", playerHealth);
            SetField(spitterBrain, "projectilePrefab", projectilePrefab);
            SetField(spitterHealth, "definition", definition);
            SetField(spitterHealth, "brain", spitterBrain);
            spitter.SetActive(true);
            for (int i = 0; i < 60 && playerHealth.CurrentHealth == 5f; i++) yield return new WaitForFixedUpdate();
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(4f));
            Object.Destroy(spitter);
            Object.Destroy(projectileObject);
        }
        Projectile BuildProjectile(Vector2 position, bool reflect)
        {
            GameObject projectileObject = new GameObject("Projectile_Spit") { layer = LayerMask.NameToLayer("Enemy") };
            projectileObject.SetActive(false);
            projectileObject.transform.position = position;
            projectileObject.AddComponent<Rigidbody2D>();
            projectileObject.AddComponent<CircleCollider2D>();
            Projectile projectile = projectileObject.AddComponent<Projectile>();
            projectileObject.SetActive(true);
            projectile.Initialize(projectileDefinition, Vector2.right, null);
            if (reflect) projectile.TakeHit(new HitInfo(1f, Vector2.right, 0f, playerObject.transform));
            return projectile;
        }
        EnemyState GetState() => (EnemyState)typeof(EnemyBrain).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activeBrain);
    }
}
