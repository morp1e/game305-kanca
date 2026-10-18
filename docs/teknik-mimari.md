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
    Audio/
    Data/           (SO asset'leri: ayarlar, boss desenleri, olay kanalları)
    Prefabs/
    Scenes/
      Bootstrap.unity
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

## 3. Sahne düzeni ve çakışma önleme

- `Bootstrap` sahnesi kalıcı sistemleri taşır (oyun durumu, ses, kamera kökü) ve bölüm sahnelerini **additive** yükler.
- **Her sahnenin tek sahibi vardır.** Sahibi olmadığın sahneyi değiştireceksen önce haber ver. Sahiplik `docs/ekip-rehberi.md` içindeki tabloda.
- Deneme yapmak için kişisel `Sandbox_<ad>` sahnesi kullanılır.
- Sahne yalnız yerleşimi taşır. Davranış prefab'ta, ayar SO'da durur. Böylece aynı anda iki kişinin aynı dosyayı değiştirmesi seyrekleşir.

## 4. Oyuncu kontrolcüsü

- **Kinematic Rigidbody2D + kendi hareket kodumuz** (`Rigidbody2D.Slide` ve `Rigidbody2D.Cast`). Gerekçe: platform oyununda kontrol hissi hassas ayar ister ve kanca çekişi hızı doğrudan devralmalı. Unity, `Slide`'ı esas olarak Kinematic gövde için tarif ediyor.
- Yerçekimi, zemin teması, coyote time, jump buffer ve köşe düzeltme kodda yazılır, değerleri `PlayerMovementTuning` SO'sundadır.
- **Girdi `Update`'te okunur, tampona yazılır, `FixedUpdate`'te tüketilir.** Input System, `FixedUpdate` içinde sorgulanınca editörde hata basıyor.
- Kontrolcünün bir "hız devralma" modu olur: kanca çekişi sırasında yerçekimi ve oyuncu girdisi devre dışı kalır, hızı kanca sistemi belirler.

## 5. Kılıç-kanca sistemi

### Davranış
1. Oyuncu fareyle nişan alır, kılıcı fırlatır.
2. Kılıç yalnız **kanca noktalarına (anchor)** saplanır. Başka yüzeye çarparsa sekip geri döner.
3. Saplanınca karakter kılıca doğru **çekilir**, varışta ivmeyle **fırlar**. Sallanma ve ip yoktur.
4. Kılıç fırlatılmışken elde değildir, geri çağrılabilir.

### Durum makinesi
```
InHand -> Thrown -> Stuck -> Pulling -> Launch -> InHand
Thrown  --(anchor dışı çarpma / menzil sonu / geri çağırma)--> Returning -> InHand
Stuck   --(geri çağırma)--> Returning
Pulling --(engel / iptal / maxPullTime)--> Launch veya Returning
```
İlk sürümde enum + switch. Durum sayısı artarsa durum sınıflarına geçilir.

### Kılıç uçuşu
Kılıcın Rigidbody'si yoktur. Her `FixedUpdate`'te `Physics2D.CircleCast(konum, swordRadius, yön, throwSpeed*dt, Anchor|Ground)` atılır ve isabet noktasına konumlanır. Hızlı nesnede tünellemeyi önler, Continuous çarpışma modunun maliyeti olmaz.

### Çekiş ve fırlama (Celeste'in açık kaynak dash kodundan uyarlandı)
- Çekiş sabit `pullSpeed` ile yapılır. İsteğe bağlı `pullSpeedCurve` (normalize mesafeye göre AnimationCurve, varsayılan düz) giriş ve çıkışı şekillendirir. `maxPullTime` emniyettir.
- Oyuncu çekiş yönünde zaten daha hızlıysa o hız korunur.
- Anchor'a `arriveDistance` kadar yaklaşınca çekiş biter: hız = `yön × pullSpeed × exitSpeedMult`, yukarı yöndeyse ayrıca `× exitUpMult`.
- Fırlamadan sonra kısa bir `postLaunchJumpWindow` açılır: bu pencerede zıplamaya basılırsa ivme zıplamayla birleşir.
- Çekiş sırasında önden `Rigidbody2D.Cast` atılır. Engel varsa çekiş durur ya da hız yüzeye teğet yönlendirilir.

### Nişan yardımı (fare)
İmleç zorla kaydırılmaz. Yalnız hedef seçimi ve nişangâh kilidi yapılır:
1. `Physics2D.OverlapCircle` ile oyuncudan `maxRange` içindeki anchor'lar toplanır.
2. Oyuncudan her adaya `Physics2D.Raycast` atılır. Arada engel varsa aday elenir.
3. Skor: `centeredness = 1 - clamp01(ekranMesafesi(imleç, anchor) / snapRadiusPx)`, artı menzil terimi. Skoru 0 olan aday seçilmez.
4. Histerezis: yeni aday mevcut hedefi ancak `switchMargin` farkla geçerse seçilir. Hedef kareden kareye titremez.
5. Hedef yoksa kılıç imleç yönüne serbest fırlatılır.

### Juice
- Saplanma anında 50 ms donma (hitstop). `Time.timeScale` yerine oyun mantığında yerel donma kullanılır.
- Çekiş yönünde 0,2 sn yönlü sarsıntı.
- Kamera sarsıntısı trauma modeliyle: saplanma +0,2, büyük çarpma +0,5, sarsıntı = trauma², Perlin gürültüsü.

### `SwordHookTuning` başlangıç değerleri
Birim: 1 unit = 1 kare. Kaynak: C = Celeste kodu (piksel/sn, 8 px = 1 kare olarak çevrildi), E = Eiserloh GDC 2016, Ö = bizim tahminimiz, oynayarak ayarlanacak.

| Parametre | Başlangıç | Birim | Kaynak |
|---|---|---|---|
| throwSpeed | 40 | u/s | Ö |
| maxRange | 10 | u | Ö |
| swordRadius | 0,15 | u | Ö |
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
| snapRadiusPx | 64 | ekran px | Ö |
| switchMargin | 0,15 | skor | Ö |

Celeste karakteri 8 px'lik karelerde koşuyor, bizim karelerimiz 16 px. Bu yüzden çevrilmiş değerler başlangıç noktasıdır, doğru değer değildir.

## 6. Kamera ve karışık çözünürlük

Piksel katman (PPU 16) ile boyalı katman (PPU 64) aynı ekranda. Pixel Perfect Camera'nın `Upscale Render Texture` modu tüm sahneyi 480×270'e indirir ve boyalı arka planı bozar, bu yüzden kapalı kalır.

**İlk haftada 30 dakikalık deneme sahnesiyle seçilecek:**
- **(a) Varsayılan aday:** tek kamera, Pixel Perfect Camera, upscale kapalı. Kamera ve piksel nesneleri 1/16 unit'e yuvarlanır, boyalı parallax katmanları serbest kayar.
- **(b) Yedek:** ayrı bir kamera piksel katmanını 480×270 render texture'a çizer, sonuç boyalı sahnenin üstüne Point filtreyle basılır.

Unity'nin 6.3 dokümanları Camera Stacking'in 2D Renderer'da çalışıp çalışmadığı konusunda iki sayfada farklı şey söylüyor. Bu yüzden karar dokümana göre değil denemeye göre verilir. Ölçüt: 1080p ve 1440p'de üç parallax katmanı kayarken titreme ve bulanıklık olup olmadığı, gözle.

Ekran çözünürlüğü politikası: 1080p ve 4K tam sayı ölçekte çalışır. 1440p gibi tam sayı olmayan ekranlarda Crop Frame `None` (biraz daha fazla dünya görünür) kullanılır.

## 7. Veri ve olaylar

- Ayarlar SO'da: `PlayerMovementTuning`, `SwordHookTuning`, `CameraTuning`, `BossPattern`.
- Sistemler arası haberleşme SO olay kanallarıyla: `OnPlayerDamaged`, `OnAnchorStuck` vb. Böylece UI ve ses, oyuncu koduna doğrudan bağlanmaz.
- SO'lar build'de **salt okunurdur**. Oyuncu ilerlemesi ayrı bir kayıt dosyasında tutulur.
- Play modunda SO'da yapılan değişiklik kalıcıdır. Ayar yaparken işe yarar, ama yanlışlıkla değiştirilen değer commit'e girer. Commit'ten önce diff'e bakın.

## 8. Git ve büyük dosyalar

- `.gitignore`: GitHub'ın resmî Unity şablonu.
- `.gitattributes`: Unity YAML dosyaları metin olarak kalır, LFS'e girmez. Görsel, ses, font ve belge dosyaları Git LFS'tedir.
- Unity'nin varsayılanları doğru: Asset Serialization = Force Text, Version Control = Visible Meta Files.
- **Git LFS kotası:** GitHub ücretsiz hesapta 10 GiB depolama ve ayda 10 GiB indirme. Her yeni klon (laboratuvar bilgisayarı dahil) indirme kotasından düşer. Ses ogg olarak eklenir, aynı dosyanın gereksiz sürümleri commit'lenmez.
- **Sahne ve prefab çakışması için Unity Smart Merge** (UnityYAMLMerge). Kurulum `docs/ekip-rehberi.md` içinde.
