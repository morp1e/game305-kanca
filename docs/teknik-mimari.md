# Teknik Mimari

**Sürüm:** v0.1 (2026-10-18) · **Sahibi:** Özenç · Bu belge kodu yazanlar içindir (Özenç, Efe, Gizmo).

## İlke

**Kod davranışı tanımlar. İçerik ve ayarlar dışarıdadır ve kod açılmadan değiştirilebilir.** Hız, süre, menzil, can, boss deseni gibi her ayar bir ScriptableObject (SO) içinde durur. Bölümler prefab ve Tilemap ile kurulur. Çizimler, adlandırma kuralıyla yerine konan dosyalardır.

## 1. Motor ve paketler

| Bileşen | Seçim | Gerekçe |
|---|---|---|
| Editör | **Unity 6000.3.26f1** (6.3 LTS) | Desteği Aralık 2027'ye kadar. 6.6 "Supported" sürüm, 6.7 çıkınca proje ortasında yükseltme gerektirir. Herkes **tam olarak bu yamayı** kullanır. |
| Şablon | Universal 2D | URP 2D Renderer ve 2D paketleriyle gelir. Built-in render hattı Unity 6.5'te deprecated. |
| Girdi | Input System 1.20.x | Eski Input Manager kullanılmaz. |
| Kamera | Cinemachine 3.1.x | 2.x kullanılmaz. Namespace `Unity.Cinemachine`. |
| Piksel art | Aseprite Importer 3.x, 2D Pixel Perfect | `.aseprite` dosyası doğrudan okunur. |
| Boyalı katman | PSD Importer 12.x | PSB/PSD katmanları. |
| Bölüm | Tilemap, 2D Tilemap Extras | Rule Tile ile otomatik kenar. |
| UI | uGUI 2.0 (TextMeshPro içinde) | Ayrı `com.unity.textmeshpro` paketi eklenmez. |
| Test | Unity Test Framework | EditMode ve PlayMode testleri. |
| Build | Windows (Mono) + WebGL | WebGL, itch.io'da tarayıcıdan oynanan sürüm için. |
| IDE | Visual Studio 2022 Community | |

## 2. Klasör yapısı

```
Assets/
  _Project/
    Art/
      Pixel/        (PPU 16, Point, sıkıştırma yok)
      Painted/      (PPU 64, Bilinear, BC7)
      Tiles/        (PPU 128, Bilinear, BC7)
    Audio/
    Data/           (SO asset'leri: ayarlar, boss desenleri, olay kanalları)
    Prefabs/
    Resources/Flow/ (SceneFlow.prefab: sahne akışı, karartma, süre/ölüm sayacı)
    Scenes/
      Title.unity, End.unity  (UiFactory üretir)
      Levels/
      Sandbox/      (kişisel deneme sahneleri: Sandbox_Ozenc, Sandbox_Efe)
    Scripts/
      Runtime/      (asmdef: Game.Runtime)
      Editor/       (asmdef: Game.Editor)
    Tests/
      EditMode/     (asmdef: Game.Tests.EditMode)
      PlayMode/     (asmdef: Game.Tests.PlayMode)
    Settings/       (URP, Input Actions, Preset'ler)
```

- Üçüncü parti içerik `_Project` dışında durur.
- Dosya ve klasör adlarında boşluk ve Türkçe karakter yok.
- Dosya başına bir sınıf, dosya adı sınıf adıyla aynı. Namespace klasörü izler: `Game.Player`, `Game.Hook`, `Game.Boss`.

### Import kuralları
Preset Manager'da klasöre göre iki varsayılan preset:
- `Art/Pixel/`: Sprite, PPU 16, Filter Point, Compression None, mipmap kapalı, Mesh Full Rect, Wrap Clamp. Aseprite Importer'ın varsayılan PPU'su 100'dür, 16'ya çekilir.
- `Art/Painted/`: Sprite, PPU 64, Filter Bilinear, BC7, mipmap kapalı, Max Size 4096.
- `Art/Tiles/`: Sprite, PPU 128, Filter Bilinear, BC7, mipmap kapalı, Max Size 2048, pivot center. Kenney 18 px karolar geçici yer tutucudur.

## 3. Sahne düzeni ve çakışma önleme

- **Her sahne tek başına Play'e basılabilir.** Bootstrap sahnesi ve additive yükleme yok (Gizmo, 2026-10-30; önceki plan buydu). Kalıcı sistemler (`SceneFlow`, `ScreenFader`, `RunStats`) `Resources/Flow/SceneFlow.prefab`'dan ilk ihtiyaçta kurulur ve `DontDestroyOnLoad` olur. Bölümler tek modda (`LoadSceneMode.Single`, async) yüklenir, geçişte ekran kararır. Gerekçe: Sandbox ve bölüm sahneleri doğrudan denenebilir; iki kişilik ekipte additive sahne yönetiminin hata yüzeyi gereksiz.
- **Her sahnenin tek sahibi vardır.** Sahibi olmadığın sahneyi değiştireceksen önce haber ver. Sahiplik `docs/ekip-rehberi.md` içindeki tabloda.
- Deneme yapmak için kişisel `Sandbox_<ad>` sahnesi kullanılır.
- Sahne yalnız yerleşimi taşır. Davranış prefab'ta, ayar SO'da durur. Böylece aynı anda iki kişinin aynı dosyayı değiştirmesi seyrekleşir.

## 4. Oyuncu kontrolcüsü

- **Dynamic Rigidbody2D, hızı biz yazarız** (gravityScale 0, yerçekimi ve tüm ivme kodda). Çarpışma ve zeminden ayırma fizik motorunda kalır. Gerekçe: kontrol hissi yine tamamen bizim kodumuzda, kanca çekişi hızı doğrudan devralır, ama temas payı ve tünelleme gibi hatalara açık kendi çarpışma kodumuzu taşımayız. 2026-10-18: ilk sürüm Kinematic + kendi Cast döngüsüyle yazılmıştı; gerçek fizik testlerinde kırılgan bulunup değiştirildi.
- Yerçekimi, zemin teması, coyote time, 0,08 sn jump buffer, değişken zıplama, hızlı düşüş, duvar kayması ve duvar zıplaması ile köşe düzeltme Celeste `Player.cs` mantığına göre uyarlanmıştır. Değerler `PlayerMovementTuning` SO'sundadır; duvar zıplaması tasarımcı tarafından kapatılabilir.
- Yatay hız Celeste kuralıyla yönetilir: aynı yönde maksimum koşu hızını aşınca `runReduce`, diğer durumlarda `runAccel` kullanılır. Bilinçli sapma: kanca fırlatma penceresindeki aşırı yatay momentum girdisizken aniden kesilmez, `runReduce` ile yavaşça azalır; kanca fırlamasının mevcut akış hissi korunur.
- **Girdi `Update`'te okunur, tampona yazılır, `FixedUpdate`'te tüketilir.** Input System, `FixedUpdate` içinde sorgulanınca editörde hata basıyor.
- Kontrolcünün bir "hız devralma" modu olur: kanca çekişi sırasında yerçekimi ve oyuncu girdisi devre dışı kalır, hızı kanca sistemi belirler.

### Gövde ve kol rig'i
- Oyuncu görseli gövde, sol elde kanca kolu ve sağ elde kılıç kolu olarak ayrı parçalardır. Kollar omuz pivotundan el ucuna uzanır; `PlayerArmRig` görseli her `Update`'te imlece yöneltir.
- Bakış yönü hareketten bağımsız olarak imlecin gövde merkezine göre yatay tarafıdır. Merkez çevresindeki `facingDeadZone` içinde önceki yön korunur; hareket yönü yalnız rig yoksa `PlayerMotor` yedeğidir.
- Sağa bakarken sol/kanca kolu gövdenin arkasında, sağ/kılıç kolu önündedir. Sola bakınca çizim sırası değişir. Sol yön aynalaması omuz X ofsetini ve kol açısını yansıtır; kol çizimi +X yönüne, pivot omuzda olacak şekilde yapılır.
- `PlayerRigTuning.angleSnapDegrees` sıfır olduğunda serbest dönüş, 22,5 olduğunda 16 yön ve 45 olduğunda 8 yön verir. Piksel art sprite'larının serbest dönüşteki görünümü gözle değerlendirilecektir; karşılaştırma kamera probuyla birlikte §6'da yapılır.

### Can ve hasar
- `PlayerHealth`, oyuncu gövdesinde `IDamageable` uygular. Temas hasarı ve diğer saldırılar `HitInfo` ile vurur; can `HealthEvent` kanalına başlangıçta ve her değişimde yayınlanır.
- Ölümcül olmayan isabet canı azaltır, `invulnerabilitySeconds` boyunca sonraki isabetleri yok sayar, kısa süreli yerel hitstop ve geri itme uygular. Geri itme kilidinde yatay girdi ve zıplama tüketilmez, yerçekimi sürer.
- Diken davranışı `PlayerHealthTuning.hazardPolicy` ile seçilir. `InstantDeath` mevcut ölüm akışını kullanır. `DamageAndReturn` can düşürür ve oyuncuyu son girdiği `HazardRespawnZone` noktasına, bu yoksa checkpoint'e geri taşır. Dönüş sırasında diken tekrarlanmaz, can sıfırlanırsa ölüm akışı çalışır.
- Gerçek ölüm sonrası `refillOnRespawn` canı doldurur. Diken dönüşü ölüm sayılmaz ve ölüm olayı yayınlamaz.

## 5. Kılıç ve kanca sistemi

### Davranış
1. Kılıç sağ elde yakın dövüş silahıdır; sol fare saldırır. Süre, vuruş penceresi, yay, şekil ve hasar `SwordTuning` SO'dadır. Sol eldeki ayrı kanca sağ fareyle imlece doğru fırlatılır.
2. Kanca, `GrappleHookTuning.hookableLayers` katmanlarındaki yüzeylere saplanır. `NonHookable` bileşeni taşıyan collider veya üst objesi kancayı geri döndürür. `pullableLayers` katmanındaki hafif `HookPullable` hedef oyuncuya sarılır; ağır hedef oyuncuyu kendine çeker ve hedef noktası takip edilir.
3. Saplanınca karakter yüzeydeki temas noktasına doğru **çekilir**, yüzeye temas ettiğinde veya varış eşiğine geldiğinde son çekiş yönünde **fırlar**. Sallanma ve ip fiziği yoktur.
4. Sağ fare basışı kancayı fırlatır; kanca Thrown, Stuck, Pulling veya Reeling durumundayken sağ fareye yeniden basmak geri çağırır.

### Durum makinesi
```
InHand -> Thrown -> Stuck -> Pulling -> Launch -> InHand
Thrown  --(NonHookable / menzil sonu / geri çağırma)--> Returning -> InHand
Stuck   --(sağ fareye yeniden basma)--> Returning
Pulling --(tutunulan yüzeye temas / varış / engel / maxPullTime)--> Launch
Pulling --(sağ fareye yeniden basma)--> Returning
Thrown --(hafif HookPullable)--> Reeling --(varış / süre sonu)--> InHand
```
İlk sürümde enum + switch. Durum sayısı artarsa durum sınıflarına geçilir.

### Kanca uçuşu
Kancanın Rigidbody'si yoktur. Her `FixedUpdate`'te `Physics2D.CircleCast` atılır; sorgu `hookableLayers` ve `pullableLayers` maskelerini birleştirir. Hızlı nesnede tünellemeyi önler, Continuous çarpışma modunun maliyeti olmaz. Menzil içinde isabet olmazsa kanca geri döner.

### Çekiş ve fırlama (Celeste'in açık kaynak dash kodundan uyarlandı)
- Çekiş sabit `pullSpeed` ile yapılır. İsteğe bağlı `pullSpeedCurve` (normalize mesafeye göre AnimationCurve, varsayılan düz) giriş ve çıkışı şekillendirir. `maxPullTime` emniyettir.
- Oyuncu çekiş yönünde zaten daha hızlıysa o hız korunur.
- Tutunulan yüzeye `arriveDistance` kadar yaklaşınca çekiş biter: hız = `son çekiş yönü × pullSpeed × exitSpeedMult`, yukarı yöndeyse ayrıca `× exitUpMult`. Oyuncu collider'ı yüzeye daha önce değerse aynı fırlama başlar.
- Fırlamadan sonra kısa bir `postLaunchJumpWindow` açılır: bu pencerede zıplamaya basılırsa ivme zıplamayla birleşir.
- Çekiş sırasında önden `Rigidbody2D.Cast` atılır. Tutunulan yüzeye veya teğet yönü olmayan bir engele değince fırlama başlar; başka engelde hız yüzeye teğet yönlendirilir.
- `pullableLayers` varsayılanı Enemy ve Default'tur. Hafif hedef oyuncunun eline doğru `lightPullSpeed` ile çekilir; oyuncu kontrolü sürer. Varışta veya `pullableMaxTime` sonunda hedef `pullableReleaseSpeed` ile bırakılır ve hasar alabilen hedefe `pullableStunDuration` sıfır hasarlı hit bilgisi gönderilir. Ağır hedef için oyuncu mevcut çekiş ve fırlama akışını kullanır; temas noktası her fizik adımında hedefi izler.

### Nişan
Kanca, `PlayerInputReader.PointerWorld` ile alınan fare dünya konumuna doğru düz fırlatılır. Hedefe yapışma veya nişan yardımı uygulanmaz. Nişangâh ve çizgi imleci izler.

### Juice
- Saplanma anında 50 ms donma (hitstop). `Time.timeScale` yerine oyun mantığında yerel donma kullanılır.
- Çekiş yönünde 0,2 sn yönlü sarsıntı.
- Kamera sarsıntısı trauma modeliyle: saplanma +0,2, büyük çarpma +0,5, sarsıntı = trauma², Perlin gürültüsü.

### `GrappleHookTuning` başlangıç değerleri
Birim: 1 unit = 1 kare. Kaynak: C = Celeste kodu (piksel/sn, 8 px = 1 kare olarak çevrildi), E = Eiserloh GDC 2016, Ö = bizim tahminimiz, oynayarak ayarlanacak.

| Parametre | Başlangıç | Birim | Kaynak |
|---|---|---|---|
| throwSpeed | 80 | u/s | Ö; Özenç oynayarak 40'tan çekti |
| maxRange | 8 | u | Ö; Özenç oynayarak 10'dan çekti |
| hookRadius | 0,15 | u | Ö |
| hookableLayers | Ground | katman maskesi | Ö |
| pullSpeed | 30 | u/s | C (240 ÷ 8) |
| pullSpeedCurve | düz 1 | eğri | Ö |
| maxPullTime | 0,6 | s | Ö |
| arriveDistance | 0,5 | u | Ö |
| exitSpeedMult | 0,667 | × | C (160 / 240) |
| exitUpMult | 0,75 | × | C |
| keepFasterIncomingSpeed | açık | | C |
| postLaunchJumpWindow | 0,1 | s | C |
| hookCooldown | 0,2 | s | C |
| recallSpeed | 50 | u/s | Ö |
| impactFreeze | 0,05 | s | C |
| pullShakeDuration | 0,2 | s | C |
| traumaOnStick / traumaOnBigImpact | 0,2 / 0,5 | 0-1 | E |
| shakeExponent | 2 | | E |
| traumaDecay | 1,0 | /s | Ö |
| pullableLayers | Enemy + Default | katman maskesi | Ö |
| lightPullSpeed | 30 | u/s | Ö, oyuncu çekişiyle aynı |
| pullableArriveDistance | 0,5 | u | Ö |
| pullableMaxTime | 2 | s | Ö |
| pullableReleaseSpeed | 1 | u/s | Ö |
| pullableStunDuration | 0,5 | s | Ö; kılıcın hazırlık + vuruşu 0,19 s, 0,2 s ile çek-ve-vur oturmuyordu |

Celeste karakteri 8 px'lik karelerde koşuyor, bizim karelerimiz 16 px. Bu yüzden çevrilmiş değerler başlangıç noktasıdır, doğru değer değildir.

### Kılıç savuruşu
Savuruş üç fazdır: hazırlık (`windupSeconds`), hasar veren vuruş (`activeSeconds`), toparlanma (`recoverySeconds`). Vuruş sırasında bıçak, omuz pivotundan uzanan bir kapsüldür (`bladeLength`, `bladeRadius`); iki fizik adımı arasındaki açı farkı `maxSweepStepDegrees` adımlarla taranır, böylece hızlı savuruş ince hedefin içinden geçmez. Aynı savuruş bir hedefe bir kez vurur.

- İsabette oyuncu ve hedef birlikte `hitstop` kadar donar; geri itme donma bittikten sonra nişan yönünde (`knockbackUpBias` kadar yukarı eğik) uygulanır.
- Savuruş başlangıçları arasında en az `swingInterval` geçer. Önceki savuruş bittikten sonra `comboWindow` içinde gelen saldırı ters yönde savrulur.
- Toparlanma fazını zıplama keser; kanca kesmez (kanca girdiyi kılıçtan önce tüketir).
- Kılıç hareket taşımaz: pogo ve oyuncuyu ileri taşıyan vuruş yoktur (Özenç, 2026-10-26).

### `SwordTuning` başlangıç değerleri
Saldırılar arası süre 0,4 s için Hollow Knight nail referans alındı (Ö). Diğer değerler oynayarak ayarlanacak.

| Parametre | Başlangıç | Birim | Kaynak |
|---|---:|---|---|
| windupSeconds / activeSeconds / recoverySeconds | 0,05 / 0,14 / 0,06 | s | Ö |
| swingInterval | 0,4 | s | Ö, Hollow Knight referansı |
| comboWindow | 0,35 | s | Ö |
| swingArcDegrees / windupPullbackDegrees | 100 / 8 | derece | Ö |
| bladeLength / bladeRadius | 1,4 / 0,12 | u | Ö |
| maxSweepStepDegrees | 4 | derece | Ö |
| damage / knockback / knockbackUpBias | 1 / 8 / 0,15 | can / dürtü / × | Ö |
| hitstop / traumaOnHit | 0,07 / 0,2 | s / 0-1 | Ö |
| inputBuffer | 0,3 | s | Ö |

## 6. Kamera ve karışık çözünürlük

Piksel katman (PPU 16) ile boyalı katman (PPU 64) aynı ekranda. Pixel Perfect Camera'nın `Upscale Render Texture` modu tüm sahneyi 480×270'e indirir ve boyalı arka planı bozar, bu yüzden kapalı kalır.

**İlk haftada 30 dakikalık deneme sahnesiyle seçilecek:**
- **(a) Varsayılan aday:** tek kamera, Pixel Perfect Camera, upscale kapalı. Kamera ve piksel nesneleri 1/16 unit'e yuvarlanır, boyalı parallax katmanları serbest kayar.
- **(b) Yedek:** ayrı bir kamera piksel katmanını 480×270 render texture'a çizer, sonuç boyalı sahnenin üstüne Point filtreyle basılır.

Unity'nin 6.3 dokümanları Camera Stacking'in 2D Renderer'da çalışıp çalışmadığı konusunda iki sayfada farklı şey söylüyor. Bu yüzden karar dokümana göre değil denemeye göre verilir. Ölçüt: 1080p ve 1440p'de üç parallax katmanı kayarken titreme ve bulanıklık olup olmadığı, gözle.

Ekran çözünürlüğü politikası: 1080p ve 4K tam sayı ölçekte çalışır. 1440p gibi tam sayı olmayan ekranlarda Crop Frame `None` (biraz daha fazla dünya görünür) kullanılır.

## 7. Veri ve olaylar

- Ayarlar SO'da: `PlayerMovementTuning`, `PlayerRigTuning`, `PlayerHealthTuning`, `GrappleHookTuning`, `SwordTuning`, `TrainingDummyTuning`, `CameraTuning`, `BossDefinition`, `BossPhase`, `BossAttack`.
- Sistemler arası haberleşme SO olay kanallarıyla: `OnPlayerDamaged`, `OnHookStuck` vb. Böylece UI ve ses, oyuncu koduna doğrudan bağlanmaz.
- SO'lar build'de **salt okunurdur**. Oyuncu ilerlemesi ayrı bir kayıt dosyasında tutulur.
- Play modunda SO'da yapılan değişiklik kalıcıdır. Ayar yaparken işe yarar, ama yanlışlıkla değiştirilen değer commit'e girer. Commit'ten önce diff'e bakın.

## 8. Git ve büyük dosyalar

- `.gitignore`: GitHub'ın resmî Unity şablonu.
- `.gitattributes`: Unity YAML dosyaları metin olarak kalır, LFS'e girmez. Görsel, ses, font ve belge dosyaları Git LFS'tedir.
- Unity'nin varsayılanları doğru: Asset Serialization = Force Text, Version Control = Visible Meta Files.
- **Git LFS kotası:** GitHub ücretsiz hesapta 10 GiB depolama ve ayda 10 GiB indirme. Her yeni klon (laboratuvar bilgisayarı dahil) indirme kotasından düşer. Ses ogg olarak eklenir, aynı dosyanın gereksiz sürümleri commit'lenmez.
- **Sahne ve prefab çakışması için Unity Smart Merge** (UnityYAMLMerge). Kurulum `docs/ekip-rehberi.md` içinde.

## 9. Arayüz ve sahne akışı

- Arayüz oyun koduna bağlanmaz, olay kanallarını dinler: `HealthHud` → `Event_PlayerHealth`; `BossHealthBar` → `Event_BossFightStarted` (adı yazar, görünür), `Event_BossHealth`, `Event_BossDefeated` (bir süre sonra gizlenir), `Event_PlayerRespawn` (dövüş sıfırlandığı için gizlenir).
- `GameHud.prefab` (sol üstte can maskeleri, üst ortada boss çubuğu, Esc ile duraklatma menüsü) her bölüm sahnesine konur. İçindeki `FlowBootstrap`, bölüm doğrudan açılsa da `SceneFlow`'u kurar: "oyun bitti" kanalı dinlenir, süre ve ölüm sayılır.
- Akış: `Title` → `SceneCatalog.levels` sırası → `End`. `Event_GameWon` (boss yenilince) bitiş sahnesini açar; `LevelExit` tetikleyicisi sıradaki sahneye geçer. Duraklatma `Time.timeScale = 0` kullanır; menü kapanırken `PlayerInputReader.ClearBuffers()` menü tıklamalarının saldırı/kanca olarak kalmasını önler.
- Görsel: 1920×1080 referanslı `CanvasScaler` (eşleme 0,5), TextMeshPro, renkler `UiPalette`'te (koyu lacivert zemin, kırık beyaz metin, tek vurgu rengi amber). Prefab'lar ve Title/End sahneleri `Tools/Kanca/Arayuzu Kur` (`Game.Editor.UI.UiFactory.BuildAll`) ile koddan üretilir; elle düzenlenmez, değişiklik fabrikada yapılır. Metinler bileşen alanıdır, kod açmadan değişir.

## 10. Bölüm verisi ve build

Bölüm yerleşimi `Assets/_Project/Data/Levels/*.txt` metin haritasında tutulur. `LevelLegend.asset` karakterleri Tilemap türü veya prefab yoluyla eşler. Harita değiştikten sonra Tools > Kanca > Bolumu Haritadan Kur ile sahne yeniden üretilir. Kurucu deterministiktir ve aynı harita aynı sahneyi üretir. `Level_01` için batchmode girişi `Game.Editor.Level.LevelBuilder.BuildLevel01` metodudur.

Build menüleri etkin Build Settings sahnelerini kullanır. `Game.Editor.Build.BuildScript.BuildWindows` Windows çıktısını `Builds/Windows/Kanca.exe`, `BuildWebGL` WebGL çıktısını `Builds/WebGL/` altına üretir. Build sırasında Development seçeneği etkinleştirilmez ve PlayerSettings değiştirilmez.

## 11. Düşmanlar

Düşman ayarları `EnemyDefinition` ScriptableObject'lerinde, mermi ayarları `ProjectileDefinition` içinde tutulur. Prefab ve içerik `Assets/_Project/Prefabs/Enemies/` altındadır. Ortak `EnemyBrain`, `EnemyHealth`, `ContactDamage` ve `HookPullable` bileşenleri kullanılır. Katman `Enemy` olur, gövde Dynamic Rigidbody2D ve FreezeRotation kullanır. Algılama için trigger kullanılmaz; mesafe ve Ground katmanına görüş ışını kullanılır.

| Prefab | Öğrettiği kanca/kılıç kararı | Can | Kanca | Başlangıç ayarları |
|---|---|---:|---|---|
| Walker | Kenardan dönen devriyeyi kancayla kes, ardından kılıçla yen | 2 | Light | 2 u/s devriye, 5 u/s hücum, 6 u algı, 0,4 s telegraph, 0,8 s hücum, 1 s toparlanma, yerçekimi 3 |
| Flyer | Havadaki telegraph'ı kancayla iptal et, dalıştan kaçın | 2 | Light | 3 u/s süzülme, 7 u algı, 0,35 s telegraph, 6 u/s dalış, 0,7 s dalış, yerçekimi 0 |
| Spitter | Görüş hattını kes veya kancayla telegraph sırasında yaklaş; mermiyi kılıçla sektir | 3 | Light | 9 u algı, 0,5 s telegraph, 6 u/s mermi, 1,8 s bekleme, yerçekimi 3 |
| Brute | Ağır hedefe kancayla çekil; kalkanı arkadan dolaş ve toparlanmada vur | 5 | Heavy | 1,5 u/s devriye, 120 derece ön kalkan, 0,6 s telegraph, 2×1,5 u saldırı kutusu, 0,2 s aktif, 1,5 s toparlanma, yerçekimi 3, geri itme direnci 1 |

`EnemyDefinition` alanları: `maxHealth`, `moveSpeed`, `chaseSpeed`, `patrolRange`, `detectionRadius`, `loseTargetRadius`, `telegraphDuration`, `attackDuration`, `recoverDuration`, `attackCooldown`, `contactDamage`, `attackDamage`, `hurtStunDuration`, `knockbackResistance`, `gravityScale`, `shieldArcDegrees`, `projectile`, `hitFlashColor`, `hitFlashDuration`, `telegraphColor`, `resetOnPlayerRespawn`; uçan düşman için `hoverAmplitude` ve `hoverFrequency`; ağır saldırı için `attackBoxSize` ve `attackBoxForward`. `ProjectileDefinition` alanları `speed`, `lifetime`, `damage`, `deflectable`, `reflectSpeedMult` ve `radius` olur. Her ayar tooltip ile Ö: oynayarak ayarlanacak olarak işaretlidir.

Tüm oynanış sayıları düşman tanımı SO'sunda düzenlenir. Başlangıç değerleri Ö: oynayarak ayarlanacak. Düşman ölünce kök nesne etkin kalır; beyin, renderer'lar, collider'lar ve fizik kapatılır. `resetOnPlayerRespawn` açıksa `Event_PlayerRespawn` ile konum, can ve durum başlangıca döner. Kancaya bağlı hafif düşman hız yazmaz ve temas hasarı vermez. Kanca sersemletmesi sıfır hasarlı `HitInfo` ile durum makinesine ulaşır. Telegraph hasar alır ama sırf kılıç isabetiyle iptal olmaz; kanca Pulled durumuna alarak iptal eder.

```text
Patrol -> Notice -> Telegraph -> Attack -> Recover -> Patrol
Patrol <-> Chase
Her etkin durum -> Hurt (hasarlı sersemletme) / Stunned (sıfır hasarlı sersemletme)
Hafif HookPullable.IsHooked -> Pulled -> Released sonrası Stunned
Her durum -> Dead
```

Brute kalkanı `ShieldMath` hesabıyla yalnız ön yaydaki saldırıyı engeller; Recover sırasında kalkan kapalıdır. Sektirilebilir mermi kılıç yönünde hızını çarpar ve düşman katmanındaki `IDamageable` nesnesine çarpar. Merminin sahibi oyuncu olduğunda oyuncuya isabet etmez. `Tools/Kanca/Dusmanlari Kur` menüsü `Game.Editor.Enemies.EnemyFactory.BuildAll` giriş noktasıyla tanımları ve prefabları üretir.

## 12. Test yazarken

Bu tuzakların her biri 2026-10-30'da gerçek bir testi yanlış kırmızıya ya da yanlış yeşile çevirdi:

- **Yerçekimi yüksek.** Oyuncu Celeste değerleriyle düşer (112,5 u/s²): zeminsiz test sahnesinde 0,2 s'de 2 karodan fazla. Oyuncu hedef olarak duracaksa altına zemin koyun ya da `PlayerMotor.ExternalControl = true` ile sabitleyin.
- **Kare saymayın.** Batchmode'da kare süresi milisaniyenin altına inebilir; "100 kare bekle" 0,01 s bile etmez. Süreyle bekleyin (`Time.time` son tarihi) ya da `WaitForFixedUpdate` sayın.
- **Nesneleri üst üste doğurmayın.** Aynı konumda doğan düşman ve oyuncu, test daha başlamadan temas hasarı üretir.
- **Reflection ile private alan atarken temel sınıfı da gezin.** `GetType().GetField` türetilmiş türde temel sınıfın private alanını bulmaz (ör. `WalkerBrain` → `EnemyBrain.definition`).
- **Geri itme ve hitstop birden fazla yerde uygulanmasın.** Kılıç, dürtüyü hitstop bittikten sonra kendisi verir; hedefin `TakeHit`'i ikinci kez itmez.
