using System;
using System.Collections;
using System.IO;
using Game.Combat;
using Game.Player;
using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.DebugTools
{
    /// <summary>
    /// Build'in görsel doğrulaması ve sunum görüntüleri için otomatik fotoğraf turu. Yalnız komut satırında
    /// <c>-kancaShots &lt;klasör&gt;</c> verilince çalışır; başlık ekranını, bölümün belli noktalarını, boss dövüşünü
    /// ve duraklatma menüsünü sırayla görüntüler, sonra oyunu kapatır. Normal oyunda hiçbir şey yapmaz.
    /// </summary>
    public sealed class ShotTour : MonoBehaviour
    {
        const string Flag = "-kancaShots";
        // Level_01.txt koordinatları (karo): giriş, kanca çukuru, ilk düşmanlar, diken koridoru, tırmanış, arena.
        static readonly (string name, Vector2 position)[] LevelStops =
        {
            ("02_giris", new Vector2(8f, 4f)),
            ("03_kanca_cukuru", new Vector2(44f, 4f)),
            ("04_ilk_dusmanlar", new Vector2(86f, 4f)),
            ("05_diken_koridoru", new Vector2(129f, 5.5f)),
            ("06_tirmanis", new Vector2(160f, 14f)),
            ("07_kalkanli", new Vector2(180f, 24f)),
        };

        string outputDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string dir = ReadFlag(Environment.GetCommandLineArgs());
            if (string.IsNullOrEmpty(dir)) return;
            var go = new GameObject("ShotTour");
            DontDestroyOnLoad(go);
            go.AddComponent<ShotTour>().outputDir = dir;
        }

        /// <summary>Bayrağın ardındaki klasörü döner; bayrak yoksa null.</summary>
        public static string ReadFlag(string[] args)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == Flag) return args[i + 1];
            return null;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outputDir);
            yield return LoadAndSettle("Title", 1.5f);
            yield return Shoot("01_baslik");

            yield return LoadAndSettle("Level_01", 1.5f);
            foreach (var stop in LevelStops)
            {
                MovePlayer(stop.position);
                yield return Settle(1.6f);
                yield return Shoot(stop.name);
            }

            // Oyuncuya iki hasar: can göstergesinde boş maskeler görünsün.
            var health = FindFirstObjectByType<PlayerHealth>();
            if (health != null) health.TakeHit(new HitInfo(2f, Vector2.right, 0f, this));

            // Arena: iç alanın sol tarafı, giriş tetikleyicisinin içi. Dövüş başlar, boss çubuğu açılır.
            MovePlayer(new Vector2(203f, 24f));
            yield return Settle(2.2f);
            yield return Shoot("08_boss_dovusu");
            yield return Settle(1.4f);
            yield return Shoot("09_boss_saldirisi");

            var pause = FindFirstObjectByType<PauseMenu>();
            if (pause != null)
            {
                pause.Pause();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Shoot("10_duraklatma");
                pause.Resume();
            }

            yield return LoadAndSettle("End", 1.0f);
            yield return Shoot("11_bitis");
            Application.Quit();
        }

        IEnumerator LoadAndSettle(string scene, float seconds)
        {
            var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            if (operation != null)
                while (!operation.isDone) yield return null;
            yield return Settle(seconds);
        }

        static IEnumerator Settle(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        static void MovePlayer(Vector2 position)
        {
            var motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null) return;
            var body = motor.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            motor.transform.position = position;
        }

        IEnumerator Shoot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}
