# Sanat ve Bölüm Tasarımı Teslim Standartları

**Sürüm:** v0.2 (2026-10-18) · **Sahibi:** Özenç · **Değişiklik yolu:** öneriyi Özenç'e ilet, onaylanınca sürüm artar.

Bu belge, çizimlerin ve bölüm tasarımlarının **yeniden çizilmeden veya düzeltilmeden** oyuna girmesi için uyulması gereken kuralları toplar. Unity açmanız gerekmez. Dosyaları ortak klasöre bırakırsınız, oyuna aktarmayı biz yaparız.

Sayıların bir kısmı **geçici** olarak işaretlenmiştir. Bunlar ilk oynanabilir prototipten sonra kesinleşir ve belge o zaman v1.0 olur. Geçici bir sayıya göre büyük iş yapmadan önce sorun.

---

## 1. Görsel yön

- **Arka plan:** Hollow Knight tarzı, elle boyanmış, yumuşak, katmanlı. Piksel değildir.
- **Karakter ve oynanış katmanı:** piksel art. Oyuncu, kılıç, düşman, boss, kanca noktası, platform ve tehlikeler bu katmandadır.
- Moodboard ayrıca paylaşılacak. Bu belge "nasıl teslim edilir" sorusunu cevaplar, "nasıl görünür" sorusunu moodboard cevaplar.

### Okunabilirlik kuralı (en önemli kural)

Oyuncu her an şunu ayırt edebilmeli: **neye basabilirim, neye kancalanabilirim, ne bana zarar verir.** Bunun için:

1. **Arka plan geri çekilir.** Oynanış katmanına göre daha düşük kontrastlı, daha az doygun ve değer (açıklık/koyuluk) olarak daha dar bir aralıkta çizilir. Uzak katmanlar daha soluk ve sadedir.
2. **İlgi noktaları belirgin şekil ve yüksek parlaklıkla öne çıkar.** (Dead Cells ekibinin kullandığı kural.)
3. **Renk kodu sabittir:**
   - Kanca noktası: oyunda başka hiçbir şeyin kullanmadığı, yalnız ona ayrılmış bir renk.
   - Tehlike (diken, zehir vb.): ayrı, sabit bir renk ailesi.
   - Bu iki rengi ilk palet toplantısında birlikte seçeceğiz.
4. **Siluet testi:** teslimden önce ekran görüntüsünü gri tonlamaya çevirin. Karakter, kanca noktası ve tehlikeler gri tonda da arka plandan ayrışıyorsa geçer.

---

## 2. Ölçü sistemi

| Kavram | Değer |
|---|---|
| Oyun ekranı | 1920×1080 |
| Piksel katmanının sanal çözünürlüğü | 480×270 (ekranda 4 kat büyütülür) |
| 1 kare (tile) | 16×16 piksel |
| Ekranda görünen alan | yaklaşık 30 kare genişlik × 17 kare yükseklik |
| Oyuncu boyu | yaklaşık 32 piksel (2 kare) |
| Boss boyu (öneri) | 3-6 kare (48-96 piksel) |

Oranları korumak için bu sayıların dışına çıkmayın. Daha büyük bir şey gerekirse önce sorun.

---

## 3. Piksel art kuralları

### 3.1 Genel
- **Kenar yumuşatma (anti-aliasing) yok.** Her piksel ya tam dolu ya tam boştur. Yarı saydam piksel, bulanıklaştırma ve yumuşak fırça kullanılmaz.
- **Ortak palet.** Piksel katmanındaki her şey aynı paletten çizilir. Palet dosyası (`.gpl` veya `.png` şerit) ilk karakter çiziminden sonra sabitlenecek. *(geçici)*
- **Önerilen araç:** Aseprite. Kaynak dosyayı `.aseprite` olarak teslim edin, çünkü Unity bu dosyayı doğrudan okuyor: katmanlar, kareler ve kare süreleri olduğu gibi aktarılıyor. Başka araç kullanırsanız PNG sprite sheet ve kaynak dosya gönderin.

### 3.2 Tuval ve ayak noktası
- **Her animasyonun kendi sabit tuvali vardır.** Bir animasyonun bütün kareleri aynı boyuttadır. Örnek: durma ve koşma 48×48, kılıç fırlatma 64×48, saldırı 96×48.
- **Ayak noktası her tuvalde aynı yerdedir:** karakterin ayakları tuvalin **en alt satırına** basar ve **yatayda tam ortadadır**. Unity'de pivot (dönme ve konum noktası) burasıdır. Bu kurala uyulmazsa animasyonlar arasında geçerken karakter "zıplar".
- Tuval genişliği ve yüksekliği **çift sayı** olsun.

### 3.3 Animasyon
- Kare süresi Aseprite'ta her kare için ayrı ayrı ayarlanabilir. Unity bu süreleri olduğu gibi okur.
- Aseprite'ta her animasyon bir **tag** olarak işaretlenir ve tag adı animasyon adıdır (`idle`, `run`, ...). Tag yönü **Forward** olmalı. Ping-pong ve reverse desteklenmiyor.
- Saldırı ve fırlatma gibi animasyonlarda **vuruşun ya da fırlatmanın gerçekleştiği kareyi** teslim notunda belirtin. Kod o kareye bağlanacak.

**İlk turda gereken oyuncu animasyonları (başlangıç önerisi, kare sayıları değişebilir):**

| Animasyon | Tag | Kare | Döngü | Not |
|---|---|---|---|---|
| Durma | `idle` | 4-6 | evet | |
| Koşma | `run` | 6-8 | evet | |
| Zıplama, yükseliş | `jump` | 2-3 | hayır | |
| Düşüş | `fall` | 2-3 | evet | |
| Yere iniş | `land` | 2-3 | hayır | |
| Kılıç fırlatma | `throw` | 3-5 | hayır | fırlatma karesini işaretleyin |
| Çekilme/uçuş | `pull` | 2-4 | evet | kılıca doğru uçarken |
| Fırlama | `launch` | 2-3 | hayır | çekilme bittikten sonraki ivmeli an |
| Kılıcı geri alma | `catch` | 2-3 | hayır | |

### 3.4 Kılıç
- Kılıç karakterden **ayrı bir sprite** olarak çizilir, çünkü fırlatılınca dönecek ve saplanacak.
- **Sağa bakar** şekilde çizilir. **Ucu tuvalin sağ kenarına değer**, saplanma noktası bu uçtur.
- İki hal: uçarken (`sword_flying`) ve saplanmış (`sword_stuck`).
- Kılıç konuşuyor. Diyalog için ileride bir portre ya da ifade seti istenecek, şimdilik gerekmiyor.

### 3.5 Kanca noktası (anchor)
- Oyuncunun ilk bakışta "kılıç buraya saplanır" diye anlayacağı ayrı bir obje.
- 16×16 veya 32×32 piksel.
- Rengi yalnız ona ayrılmıştır (bkz. okunabilirlik kuralı).
- Üç hal: boşta (`idle`), hedeflenmiş, yani imleç üstündeyken (`targeted`), kılıç saplıyken (`active`).

### 3.6 Zemin ve platform (tileset)
- 16×16 karelerden oluşan tileset.
- Kenar, köşe, iç köşe ve dolgu kareleri ayrı ayrı çizilir ki zemin her şekilde döşenebilsin. Hangi parçaların gerektiğini ilk tileset'ten önce bir şablonla vereceğiz. *(geçici)*

---

## 4. Boyalı arka plan kuralları

- **Çözünürlük kuralı:** arka planda **1 kare = 64 piksel** olarak çizilir. Böylece 1080p ekranda arka plan tam netlikte, 1:1 görünür.
  - Ekran yüksekliği 17 kare = **1080 piksel**.
  - Bir odanın arka planı: **genişlik = oda genişliği (kare) × 64 piksel, yükseklik = oda yüksekliği (kare) × 64 piksel.**
  - Örnek: 60 kare genişlik, 17 kare yükseklikte bir oda → 3840×1080 piksel.
- **Katmanlara ayrılmış** teslim edilir. Her katman **saydam arka planlı ayrı bir PNG** olur. Bu katmanlarla derinlik (parallax) efekti yapıyoruz.
  - `far`: en uzak (gökyüzü, uzak yapılar). En az hareket eden katman, odadan daha dar çizilebilir.
  - `mid`: orta mesafe.
  - `near`: oynanış katmanının hemen arkası.
  - `fg`: ön plan, oynanış katmanının **önünde** duran süsler (sarmaşık, sis, sütun). Oyuncuyu ve kanca noktalarını **asla kapatmamalı**.
- **Tek dosya en fazla 4096 piksel genişlikte olabilir.** Daha geniş katmanları sırayla birbirine değen parçalara bölün: `_01`, `_02`, ...
- **Kaynak dosya:** katmanları korunmuş PSD/PSB (Photoshop'ta "Maximize Compatibility" açık) veya `.kra` (Krita).
- Arka planda oyuncunun basabileceği bir şeymiş gibi görünen yüzey çizmeyin. Basılabilir her şey piksel katmanındadır.

---

## 5. Dosya adları ve teslim

### 5.1 Format
- **PNG, sRGB, 8 bit, saydamlıklı.** JPG kullanılmaz.
- Piksel art PNG'leri **1× ölçekte** teslim edin, büyütmeyin. (32 piksellik karakter 32 piksel olarak gelsin.)
- Kaynak dosya her zaman PNG'nin yanında gelir.

### 5.2 Adlandırma

Desen: **`tur_varlik_eylem_varyant.png`** (varyant isteğe bağlı)

- Yalnız **küçük harf, İngilizce karakter, rakam ve alt çizgi.** Türkçe karakter (ş, ğ, ı, ö, ü, ç), boşluk ve büyük harf yok.
- Animasyonlu dosyada eylem kısmı yazılmaz, tag'ler animasyon adını taşır: `char_player.aseprite`.

| Tür öneki | Anlamı | Örnek |
|---|---|---|
| `char_` | karakter, düşman, boss | `char_player.aseprite`, `char_boss_idle.png` |
| `prop_` | obje | `prop_sword_flying.png`, `prop_anchor_targeted.png` |
| `tile_` | tileset | `tile_cave_ground.png` |
| `bg_` | boyalı arka plan katmanı | `bg_cave_far_01.png`, `bg_cave_fg_02.png` |
| `fx_` | efekt | `fx_impact_spark.png` |
| `ui_` | arayüz | `ui_health_full.png` |
| `portrait_` | diyalog portresi | `portrait_sword_angry.png` |

### 5.3 Teslim klasörü

Ortak Drive klasörü: `teslim/<adiniz>/<YYYY-MM-DD>/`. İçine:
1. Export dosyaları (PNG).
2. Kaynak dosyaları (`.aseprite`, `.psd`, `.kra`).
3. Kısa bir teslim notu (`not.txt`): ne teslim edildi, hangi sürüm, fırlatma/vuruş kareleri, bilinen eksikler.
4. Kullandığınız hazır bir şey varsa (fırça hariç: doku, font, referans fotoğraf), kaynağını ve lisansını nota yazın.

Teslimden sonra dosyayı biz oyuna alırız ve sahnede kontrol ederiz. Bir sorun varsa size bu belgedeki ilgili maddeyle döneriz.

### 5.4 Teslim öncesi kontrol listesi
- [ ] Dosya adı kurala uyuyor.
- [ ] PNG, saydamlıklı, 1× ölçek.
- [ ] Piksel art'ta yarı saydam piksel yok.
- [ ] Animasyonda bütün kareler aynı tuval boyutunda, ayak noktası alt satırın ortasında.
- [ ] Arka plan katmanları ayrı, en fazla 4096 piksel genişlikte.
- [ ] Siluet testi geçti.
- [ ] Kaynak dosya ve teslim notu eklendi.

---

## 6. Bölüm tasarımı kuralları

Bölüm tasarımcıları kâğıtta veya çizim programında çalışır. Tasarımı Unity'ye biz kurarız.

- **Kareli kâğıt kullanın: 1 kutu = 1 kare (16 piksel).**
- Ekran 30×17 kutu. Bir bölüm birden fazla ekran olabilir, kamera oyuncuyu takip eder.
- Oyuncu **2 kutu** boyunda.
- **Ölçüler (geçici, prototipten sonra kesinleşir):**
  - Kanca menzili: yaklaşık **10 kutu**.
  - Zıplama yüksekliği ve mesafesi: prototipten sonra verilecek.
- **Kanca noktaları bölümün omurgasıdır.** Kılıç yalnız işaretli kanca noktalarına saplanır, düz duvara saplanmaz. Oyuncunun nereden nereye uçacağına siz karar verirsiniz. Her kanca noktası için oyuncunun hangi yönden yaklaşıp hangi yöne fırlayacağını bir okla gösterin.

### İşaret dili

| İşaret | Anlamı |
|---|---|
| dolu kutu | zemin/duvar |
| `=` | içinden alttan geçilebilen tek yönlü platform |
| `^` | tehlike (diken vb.) |
| `A` | kanca noktası |
| `→` | kanca sonrası beklenen fırlama yönü |
| `E` | düşman |
| `S` | başlangıç |
| `C` | checkpoint |
| `X` | çıkış |
| `B` | boss arenası girişi |
| `?` | sır/gizli alan |

Her sayfanın köşesine bölüm adını, sürümünü ve tarihi yazın. Fotoğrafını ya da taramasını `teslim/` klasörüne koyun.
