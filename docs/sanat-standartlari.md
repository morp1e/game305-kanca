# Sanat ve Bölüm Tasarımı Teslim Standartları

**Sürüm:** v0.3 (2026-11-15) · **Sahibi:** Özenç · **Değişiklik yolu:** öneriyi Özenç'e ilet, onaylanınca sürüm artar.

**v0.3'te ne değişti:** Kılıç artık fırlatılmıyor. Sağ elde yakın dövüş silahı (3 vuruşlu kombo, sol tık), kanca ise sol elde ayrı bir alet (sağ tık). Bu yüzden eski fırlatma animasyonları (`throw`, `pull`, `launch`, `catch`) ve uçan/saplanmış kılıç sprite'ları (`sword_flying`, `sword_stuck`) kaldırıldı. Oyuncu tablosu kolsuz gövdeye göre yenilendi (3.3). Yeni bölümler: kollar ve omuz soketi (3.4), kılıç savuruşu efekti (3.5), kanca başı (3.6), düşmanlar ve boss (4), yer tutucu paketler ve üstünü boyama (6). Karo ölçüleri arasındaki görünür çelişki (128 px karo ile 16 px karo) 2.1'de açıklandı. Kanca menzili 8 kareye güncellendi. Bölüm numaraları kaydı.

Bu belge, çizimlerin ve bölüm tasarımlarının **yeniden çizilmeden veya düzeltilmeden** oyuna girmesi için uyulması gereken kuralları toplar. Unity açmanız gerekmez. Dosyaları ortak klasöre bırakırsınız, oyuna aktarmayı biz yaparız.

Sayıların bir kısmı **geçici** olarak işaretlenmiştir. Bunlar ilk oynanabilir prototipten sonra kesinleşir ve belge o zaman v1.0 olur. Geçici bir sayıya göre büyük iş yapmadan önce sorun.

---

## 1. Görsel yön

- **Arka plan:** Hollow Knight tarzı, elle boyanmış, yumuşak, katmanlı. Piksel değildir.
- **Karakter ve oynanış katmanı:** piksel art. Oyuncu, kılıç, kanca, düşman, boss, platform ve tehlikeler bu katmandadır.
- Moodboard ayrıca paylaşılacak. Bu belge "nasıl teslim edilir" sorusunu cevaplar, "nasıl görünür" sorusunu moodboard cevaplar.

### Okunabilirlik kuralı (en önemli kural)

Oyuncu her an şunu ayırt edebilmeli: **neye basabilirim, neye kancalanabilirim, ne bana zarar verir.** Bunun için:

1. **Arka plan geri çekilir.** Oynanış katmanına göre daha düşük kontrastlı, daha az doygun ve değer (açıklık/koyuluk) olarak daha dar bir aralıkta çizilir. Uzak katmanlar daha soluk ve sadedir.
2. **İlgi noktaları belirgin şekil ve yüksek parlaklıkla öne çıkar.** (Dead Cells ekibinin kullandığı kural.) Düşmanlar, mermileri ve saldırı efektleri arka plandan daha doygun çizilir.
3. **Renk kodu sabittir:**
   - Kanca: kancanın tutup tutmadığı yüzeyler oyuncuya tek bir renk diliyle anlatılır. Hangi tarafın (kancalanan mı, kancalanmayan mı) işaret rengi alacağı ilk palet toplantısında seçilecek. *(geçici, bkz. 3.7)*
   - Tehlike (diken, zehir vb.): ayrı, sabit bir renk ailesi. Düşman saldırı efektleri de aynı aileden seçilir ki "bu renk acıtır" tek bir dil olsun.
   - Bu renkleri ilk palet toplantısında birlikte seçeceğiz.
4. **Siluet testi:** teslimden önce ekran görüntüsünü gri tonlamaya çevirin. Karakter, kancalanabilir yüzeyler ve tehlikeler gri tonda da arka plandan ayrışıyorsa geçer.

---

## 2. Ölçü sistemi

| Kavram | Değer |
|---|---|
| Oyun ekranı | 1920×1080 |
| Piksel katmanının sanal çözünürlüğü | 480×270 (ekranda 4 kat büyütülür) |
| 1 kare (tile) | 16×16 piksel (piksel katmanında) |
| Ekranda görünen alan | yaklaşık 30 kare genişlik × 17 kare yükseklik |
| Oyuncu boyu | yaklaşık 32 piksel (2 kare) |
| Boss boyu (öneri) | 3-6 kare (48-96 piksel) |

Oranları korumak için bu sayıların dışına çıkmayın. Daha büyük bir şey gerekirse önce sorun.

### 2.1 Bir kare kaç piksel? (karo ölçüleri neden farklı görünüyor)

Belgede üç farklı sayı geçiyor: 16, 64 ve 128 piksel. Çelişki değildir. Hepsi **aynı kareyi** anlatır: oyunda **1 kare = 1 birim**. Değişen, o karenin içine kaç piksel çizildiğidir. Buna PPU (birim başına piksel) denir.

| Katman | Bir kare kaç piksel çizilir | Nerede anlatılıyor |
|---|---|---|
| Karakter ve oynanış (piksel art) | 16 px (PPU 16) | 3. bölüm |
| Boyalı arka plan | 64 px (PPU 64) | 5. bölüm |
| Boyalı bölüm karoları | 128 px (PPU 128) | aşağıda |

**Boyalı bölüm karoları:** seviye karoları `Art/Tiles/` altında 128×128 px boyanır; bir karo 1 Unity unit'tir. Sprite, PPU 128, Filter Bilinear, BC7, mipmap kapalı, Max Size 2048 ve pivot center kullanılır. Kenney 18 px karolar geçici yer tutucudur.

Yani 128 px boyalı bir karo ile 16 px piksel art bir karo oyunda aynı yeri kaplar; boyalı olan sadece daha sık çizilmiştir. Zeminin son hali boyalı karo mu, 16 px piksel tileset mi (3.9) olacak, henüz kesin değil. *(geçici)*

**Yer tutucu evre istisnası:** şu an yerleştirdiğimiz GothicVania karakterleri bizim standardımızdan iridir (oyuncu yaklaşık 48 px, standart 32 px). Onları yeniden çizmeden oyuna oturtmak için **geçici olarak farklı bir PPU** kullanıyoruz: sprite boyu, karakterin çarpışma kutusuna oturacak şekilde seçiliyor (örnek: 48 px'lik oyuncu 1,8 birimlik çarpışma kutusuna). Bunun bedeli, bu karakterlerin piksellerinin ızgarayla tam hizalanmamasıdır. Boyama evresinde karakterlerin hangi boyda çizileceğine ve PPU 16 standardına dönüşe birlikte karar vereceğiz. *(geçici)*

---

## 3. Piksel art kuralları

### 3.1 Genel
- **Kenar yumuşatma (anti-aliasing) yok.** Her piksel ya tam dolu ya tam boştur. Yarı saydam piksel, bulanıklaştırma ve yumuşak fırça kullanılmaz.
- **Ortak palet.** Piksel katmanındaki her şey aynı paletten çizilir. Palet dosyası (`.gpl` veya `.png` şerit) ilk karakter çiziminden sonra sabitlenecek. *(geçici)*
- **Araç:** Aseprite. Kaynak dosyayı `.aseprite` olarak teslim edin, çünkü Unity bu dosyayı doğrudan okuyor: katmanlar, kareler ve kare süreleri olduğu gibi aktarılıyor. Başka araç kullanırsanız PNG sprite sheet ve kaynak dosya gönderin.

### 3.2 Tuval ve ayak noktası
- **Her animasyonun kendi sabit tuvali vardır.** Bir animasyonun bütün kareleri aynı boyuttadır. Örnek: durma ve koşma 48×48, kılıç savuruşu 48×48 (bitiriş 64×64).
- **Ayak noktası her tuvalde aynı yerdedir:** karakterin ayakları tuvalin **en alt satırına** basar ve **yatayda tam ortadadır**. Unity'de pivot (dönme ve konum noktası) burasıdır. Bu kurala uyulmazsa animasyonlar arasında geçerken karakter "zıplar".
- Tuval genişliği ve yüksekliği **çift sayı** olsun.

### 3.3 Animasyon
- Kare süresi Aseprite'ta her kare için ayrı ayrı ayarlanabilir. Unity bu süreleri olduğu gibi okur.
- **Temel hız önerisi 12 fps** (kare başına yaklaşık 83 ms). Teslagrad karakter animasyonlarının çoğunu bu hızda yapmış: daha az kareyle geniş hareket seti. Kılıç savuruşu efekti bunun istisnasıdır (3.5).
- Aseprite'ta her animasyon bir **tag** olarak işaretlenir ve tag adı animasyon adıdır (`idle`, `run`, ...). Tag yönü **Forward** olmalı. Ping-pong ve reverse desteklenmiyor.
- **Tag adları sözleşmedir.** Oyun animasyonu tag adıyla bulur. Bu belgedeki adları harfi harfine kullanın; aynı adla teslim edilen yeni çizim oyunda eskisinin yerine kendiliğinden geçer (bkz. 6.2).
- Düşman saldırısı gibi animasyonlarda **vuruşun gerçekleştiği kareyi** teslim notunda belirtin. Kod o kareye bağlanacak.

**Oyuncu animasyonları (başlangıç önerisi, kare sayıları değişebilir):**

Oyuncu gövdesi ve kollar ayrı teslim edilir: gövde animasyon dosyası **kolsuz** çizilir, kollar ayrı sprite'tır (3.4). Kılıç vuruşu kolda yapıldığı için gövdede saldırı animasyonu zorunlu değildir.

| Animasyon | Tag | Kare | Döngü | Not |
|---|---|---|---|---|
| Durma | `idle` | 4-6 | evet | |
| Koşma | `run` | 6-8 | evet | |
| Zıplama, yükseliş | `jump` | 2-3 | hayır | |
| Düşüş | `fall` | 2-3 | evet | |
| Yere iniş | `land` | 2-3 | hayır | |
| Duvarda kayma | `wall_slide` | 2-3 | evet | kare sayısı geçici; yüzü duvardan dışa |
| Hasar alma | `hurt` | 2-3 | hayır | kare sayısı geçici |
| Ölüm | `death` | geçici | hayır | son kare yerde kalan poz |
| Saldırıda öne eğilme | `attack_lean` | 2-3 | hayır | **isteğe bağlı**; kılıç vuruşunda gövdenin hafif öne yüklenmesi. Kare sayısı geçici |

### 3.4 Kollar ve omuz soketi

Oyuncunun iki kolu vardır ve oyunda fare imlecine doğru döner: **sağ kolda kılıç**, **sol kolda kanca**. Sağa bakarken kanca kolu gövdenin arkasında, kılıç kolu önündedir; sola dönünce oyun bunu kendisi aynalar. Siz yalnız sağa bakan hali çizersiniz.

**Kol sprite'ı:**
- Kol **sağa bakar** şekilde, yatay uzanmış çizilir: **omuz solda, el sağda.**
- Pivot (dönme noktası) **omuzdur.** Omuz, tuvalin sol kenarında ve dikeyde ortada durur ki kol omuz etrafında dönsün.
- Kol boyu kabaca 10-14 piksel. *(geçici)*
- Kılıç kolu: kol ve elindeki kılıç birlikte çizilir. Kılıcın omuzdan ucuna erişimi oynanarak ayarlanıyor: kayıtlı değer 1,4 kare (yaklaşık 22 piksel), son denemede 2,3 kare (yaklaşık 37 piksel). Çizmeden önce güncel değeri sorun. *(geçici)*
- Kanca kolu: kancayı tutan el. Fırlayan kanca başı ayrı sprite'tır (3.6).
- Kol oyunda serbest açıyla döner; piksel art dönerken kenarları basamaklanabilir. Gerekirse 16 yön (22,5° adım) ön çizilmiş kol isteyebiliriz; karar oyunda gözle verilecek. Şimdilik her kol için **tek sprite** yeter. *(geçici)*

**Omuz soketi (gövde dosyasında):**

Gövde idle veya run sırasında nefes alıp inip kalkarken omuz da yer değiştirir. Kolların gövdeden kopmaması için oyunun **her gövde karesinde omzun nerede olduğunu** bilmesi gerekir. Aseprite'ın "slice" özelliğini kullanmıyoruz, çünkü Unity onu okumuyor. Onun yerine:

1. Gövde dosyasına iki yeni katman ekleyin: `socket_front` (kılıç kolunun omzu, öndeki) ve `socket_back` (kanca kolunun omzu, arkadaki). *(katman adları geçici)*
2. Her katmanda, **her karede tam bir piksel** boyayın: omzun dönme noktası. Renk fark etmez, tam opak olsun.
3. Gövde inip kalktıkça bu pikseli de kareyle birlikte taşıyın. Omuz yerinde duruyorsa piksel de yerinde kalır, ama her karede yine bulunmalıdır.
4. Bu iki katmanı teslimde **görünmez** (göz kapalı) bırakın ki sprite'a karışmasın. Okumayı biz yaparız.
5. Bu kural her gövde tag'i için geçerli: `idle`, `run`, `jump`, `fall`, `land`, `wall_slide`, `hurt`, `death` ve varsa `attack_lean`.

**Örnek:** 48×48 tuvalde ayak noktası alt satırın ortasıdır. `idle` 1. karede öndeki omuz x=27, y=30 noktasında (Aseprite'ta sol üst köşe 0,0; y aşağı doğru artar). 2. karede gövde bir piksel çöküyor, omuz x=27, y=31'e iniyor: `socket_front` katmanındaki piksel de bir aşağı iner. `socket_back` aynı mantıkla arka omzu izler. (Sayılar yalnız örnektir.)

### 3.5 Kılıç savuruşu (smear)

Her vuruşta kılıcın arkasında kısa bir savuruş izi (smear) görünür. Bu iz kolun ve kılıcın üstüne binen ayrı bir efekttir.

- **Dosya:** `fx_sword_slash.aseprite`
- **Tag'ler (kombonun üç vuruşu):**

| Tag | Vuruş | Yön | Oyundaki görünür süre | Yay |
|---|---|---|---|---|
| `slash1` | 1. vuruş | yukarıdan aşağı | 120 ms | yaklaşık 105° |
| `slash2` | 2. vuruş | aşağıdan yukarı | 120 ms | yaklaşık 110° |
| `slash3` | 3. vuruş, ağır bitiriş | geçici | 160 ms | yaklaşık 145° |

- **Sağa doğru çizilir.** Oyun efekti imlecin yönüne çevirir. Yay, yatay eksenin üstüne ve altına eşit yayılır.
- **Pivot sol orta = omuz.** Tuvalin sol kenarının ortası omuz noktasıdır; iz oradan sağa doğru açılır.
- **Tuval:** yaklaşık 48×48, bitiriş (`slash3`) 64×64. İz kılıcın erişimine oturmalı; erişim değişirse (3.4) tuval de değişir. *(geçici)*
- **Kare:** 3-4 kare, her biri yaklaşık 30-40 ms. Bu, 12 fps kuralının bilinçli istisnasıdır: iz göz açıp kapayana kadar görünür.
- **Toplam süre yukarıdaki görünür süreyi aşmasın.** Aşan kareler oyunda kesilir. Örnek: 3 kare × 40 ms = 120 ms olur.
- Yay açıları ve süreler oynanarak ayarlanıyor. *(geçici)*
- **İsabet kıvılcımı** ayrı efekttir: `fx_impact_spark.aseprite`. Şu an oyunda parçacıkla yapılıyor; çizilirse onun yerine geçer. *(geçici)*

### 3.6 Kanca başı
- Kanca başı karakterden **ayrı bir sprite** olarak çizilir, çünkü fırlatılınca uçacak ve yüzeye saplanacak.
- **Sağa bakar** şekilde çizilir. **Ucu tuvalin sağ kenarına değer**, saplanma noktası bu uçtur.
- Uçarken ve saplanmışken ayrı hal gerekip gerekmediği açık. *(geçici)*
- Kılıç konuşuyor. Diyalog için ileride bir portre ya da ifade seti istenecek, şimdilik gerekmiyor.

### 3.7 Kancalanabilir yüzeyler
- Kanca artık ayrı bir obje gerektirmiyor: **kancalanabilir zemine ve duvara** saplanır. Kancanın tutmadığı yüzeyler ayrı karo olarak işaretlenir ve kancayı geri döndürür.
- Bu iki yüzey türü gözle ayırt edilebilmeli (bkz. okunabilirlik kuralı). Hangisinin işaret rengi alacağı palet toplantısında seçilecek. *(geçici)*
- v0.2'deki ayrı "kanca noktası" objesi (16×16 veya 32×32, `idle` / `targeted` / `active` halleri) şimdilik oyunda yok. Çizmeden önce sorun.

### 3.8 Kanca zinciri
- Bir zincir bağlantısını veya yatay döşenebilen 2-3 bağlantılık kısa bir parçayı çizin.
- Sprite PPU 16, Filter Point, Mesh Type Full Rect ve Wrap Mode Repeat olmalı. Unity sprite'ı uç noktalar arasındaki mesafe boyunca yatay döşer.
- Boss'un kalkan zinciri de aynı yolla çizilir (4.3).

### 3.9 Zemin ve platform (tileset)
- 16×16 karelerden oluşan tileset.
- Kenar, köşe, iç köşe ve dolgu kareleri ayrı ayrı çizilir ki zemin her şekilde döşenebilsin. Hangi parçaların gerektiğini ilk tileset'ten önce bir şablonla vereceğiz. *(geçici)*
- Boyalı 128 px karolarla ilişkisi için bkz. 2.1.

---

## 4. Düşmanlar ve boss

### 4.1 Her düşmanda gereken animasyonlar

Her düşman aynı yedi tag'le teslim edilir. Hepsi ayrı, Forward yönlü tag'dir.

| Animasyon | Tag | Döngü | Not |
|---|---|---|---|
| Durma | `idle` | evet | |
| Yürüme (uçanlarda uçuş) | `walk` | evet | |
| Hazırlık (uyarı) | `windup` | hayır | 12 fps'te 6-8 kare, aşağıya bakın |
| Saldırı | `attack` | hayır | **vuruş karesini işaretleyin** |
| Toparlanma | `recover` | hayır | saldırıdan sonra açık kaldığı an |
| Hasar alma | `hurt` | hayır | |
| Ölüm | `death` | hayır | son kare yerde kalan poz |

- `stagger` (sendeleme) **yalnız orta ve büyük düşmanlarda** gerekir; örneğin kalkanlı Brute ve boss. Küçük düşmanlarda `hurt` yeter. *(hangi düşmanın orta sayılacağı geçici)*
- Ayak noktası kuralı (3.2) düşmanlar için de geçerlidir.

### 4.2 Hazırlık (windup) neden 6-8 kare

Hazırlık, oyuncuya "şimdi vuracağım" diyen andır. Oyuncunun görüp tepki vermesine yetecek kadar uzun olmalı:

**hazırlık süresi = oyuncunun tepki süresi (yaklaşık 0,25 sn) + oyuncunun kaçış hareketinin süresi + zorluk payı**

İlk bölge düşmanları için bu yaklaşık **0,5-0,7 saniye** eder (Hollow Knight'taki Hornet'in hazırlıkları da bu aralıkta ölçülmüş). 12 fps'te bu **6-8 kare**dir. Sonraki bölgelerde pay azalır, hazırlık kısalır. *(geçici, oynanarak ayarlanır)*

Hazırlığı okunur, vuruşu ani yapmak için:
- Hazırlık pozları uzun ve net olsun; silah ya da göz bu sırada parlayabilir (parlaklık artışı uyarıyı güçlendirir).
- Saldırıda anahtar pozları elle çizin. Ara kare gerekiyorsa anahtar pozun **önüne ya da arkasına** ekleyin, iki pozun tam ortasına değil (Dead Cells kuralı). Hızlı saldırının aktif karesi tek kare olabilir.
- Hazırlığın süresini oyunda kod sayar; animasyondan istenen bu süreyi dolduracak 6-8 kare. Fark çıkarsa ayarlamayı biz yaparız.

### 4.3 Boss (Zincirli Bekçi)

Boss'un saldırıları ve süreleri `docs/boss-zincirli-bekci.md` belgesinde. Görsel teslim kuralları:

- **Parça parça çizilir.** Gövde, **kalkan** ve **zincir** ayrı katmanlardır. Kalkan dövüşte gövdeden sökülüp geri döndüğü için kendi pivotu olan ayrı bir parçadır. Zincir, kanca zinciri gibi döşenen bir halka sprite'ıdır (3.8).
- **Pozlar elle çizilir.** Uzun süre ekranda kalan pozlar (idle, her saldırının hazırlık ve vuruş pozu) tam kare olarak elle çizilir. Parçaların döndürülmesi yalnız bu pozların arasındaki geçişlerde kullanılır.
- **Kemik deformasyonu yok.** Sprite'lar bükülmez, esnetilmez; piksel art bunu kaldırmıyor.
- Boss'un her saldırısı kendi `windup` / `attack` / `recover` üçlüsünü ister. Tag adları saldırı listesi kesinleşince verilecek. *(geçici)*
- `stagger` gerekir: kalkanı sökülen boss sendeler.
- Kalkanın gevşek hali oyunda şu an renk değişimiyle (sarı) gösteriliyor.
- **Boyut:** standart boss boyu 3-6 kare (48-96 px); boss belgesi 48×48 hedefliyor. Yer tutucu boss bundan çok büyük (yaklaşık 144 px). Son boyut boyama evresinde kesinleşecek. *(geçici)*

---

## 5. Boyalı arka plan kuralları

- **Çözünürlük kuralı:** arka planda **1 kare = 64 piksel** olarak çizilir. Böylece 1080p ekranda arka plan tam netlikte, 1:1 görünür. (Diğer katmanlarla ilişkisi için bkz. 2.1.)
  - Ekran yüksekliği 17 kare = **1080 piksel**.
  - Bir odanın arka planı: **genişlik = oda genişliği (kare) × 64 piksel, yükseklik = oda yüksekliği (kare) × 64 piksel.**
  - Örnek: 60 kare genişlik, 17 kare yükseklikte bir oda → 3840×1080 piksel.
- **Katmanlara ayrılmış** teslim edilir. Her katman **saydam arka planlı ayrı bir PNG** olur. Bu katmanlarla derinlik (parallax) efekti yapıyoruz.
  - `far`: en uzak (gökyüzü, uzak yapılar). En az hareket eden katman, odadan daha dar çizilebilir.
  - `mid`: orta mesafe.
  - `near`: oynanış katmanının hemen arkası.
  - `fg`: ön plan, oynanış katmanının **önünde** duran süsler (sarmaşık, sis, sütun). Oyuncuyu ve kancalanabilir yüzeyleri **asla kapatmamalı**.
- **Tek dosya en fazla 4096 piksel genişlikte olabilir.** Daha geniş katmanları sırayla birbirine değen parçalara bölün: `_01`, `_02`, ...
- **Kaynak dosya:** katmanları korunmuş PSD/PSB (Photoshop'ta "Maximize Compatibility" açık) veya `.kra` (Krita).
- Arka planda oyuncunun basabileceği bir şeymiş gibi görünen yüzey çizmeyin. Basılabilir her şey piksel katmanındadır.

---

## 6. Yer tutucu paketler ve üstünü boyama

Mekanikleri düzgün hale getirene kadar karakterler ve düşmanlar için hazır, ücretsiz paketler kullanıyoruz. Sonra siz bunların üstünü boyayıp yeniden çizeceksiniz.

### 6.1 Hangi rol hangi paketten

Hepsi ansimuz'un **GothicVania** serisinden, OpenGameArt'tan, **CC0** lisanslı (serbest kullanım, atıf zorunlu değil). Kullandığımız dört paket: Church, Patreon Collection, Swamp, Cemetery.

| Rol | Yer tutucu | Durum |
|---|---|---|
| Oyuncu | Patreon Collection, **Gothic Hero** (pelerinli, kılıçlı, yaklaşık 48 px) | seçildi |
| Boss | Patreon Collection, **Demon** (zincirli gülleler, kanatlı, yaklaşık 144 px) | seçildi |
| Walker | aday: Church, burning ghoul | kesin değil |
| Spitter | aday: Church, wizard (ateş topuyla) | kesin değil |
| Flyer | adaylar: Church angel ya da Swamp / Patreon ghost | kesin değil |
| Brute | adaylar: Swamp Thing, Cemetery skeleton (giyinik), Patreon Hell-Hound | kesin değil |

Bilmeniz gereken iki ödün:
- Paketlerde **ayrı kol yok.** Yer tutucu evrede gövde sprite'ı kollu kalıyor, oyunun kolları ve kılıç üstüne çiziliyor. Kolsuz gövde, ayrı kollar ve omuz soketi (3.4) boyama evresinin işi.
- Paket karakterleri bizim standardımızdan iri; geçici olarak farklı PPU ile oturtuluyor (2.1).

### 6.2 Üstünü boyama: 7 adım (Aseprite)

1. **Kaynağı aç.** Paketin `.aseprite` dosyası varsa onu açın. Yoksa PNG şeridi `File > Import Sprite Sheet` ile hücre boyunu girerek açın. PNG kare süresi taşımaz: süreleri paketin açıklamasından `Frame > Frame Properties` (`P`) ile elle yazın ve tag'leri bu belgedeki adlarla yeniden kurun.
2. **Tuvali sabitle.** Tuvali animasyonun standart tuvaline genişletin, ayak noktasını alt satırın ortasına hizalayın (3.2). Bir animasyonun tuval boyu bir daha değişmesin; oyun pivotu tuvaldeki konumdan alıyor.
3. **Orijinali alta al.** Orijinali kilitli bir alt katmanda ya da referans katmanında (`Layer > New > New Reference Layer from File`) bırakın, yeni çizimi üst katmanda yapın. Onion skin (`F3`) açık olsun.
4. **Önce renk.** Dosyayı Indexed moda alıp ekip paletini yükleyin (`Options > Load Palette`), gerekirse renkleri `Edit > Replace Color` (`Shift+R`) ile eşleyin.
5. **Siluet ve oranı koru, sonra detayı yeniden çiz.** Karakterin boyu, çarpışma kutusuna göre oranı ve duruşu aynı kalsın; oynanış bunlara göre ayarlandı.
6. **Kare sürelerine dokunma.** Zamanlamayı değiştirmek ayrı bir karardır, önce sorun. Özellikle düşmanın `windup` süresi oyuncunun tepki süresine göre kuruldu (4.2).
7. **Orijinali kaldır, teslim et.** Orijinal katmanı teslimden önce silin ya da gizleyin, `.aseprite` dosyasını teslim edin.

**Aynı ad, aynı tag:** dosyayı yer tutucunun yerine geçecek adla (7.2) ve bu belgedeki tag adlarıyla teslim ederseniz oyunda kendiliğinden yer değiştirir; kod değişmez. Aktarmayı biz yaparız. Tag adı farklıysa animasyon oynamaz.

### 6.3 Lisans kuralları
- **CC0:** serbest. Yine de teslim notuna kaynağı yazın (7.3, madde 4).
- **CC BY:** kredi, lisans bağlantısı ve "Necrosword ekibi tarafından değiştirildi" notu gerekir.
- **CC BY-SA ve CC BY-NC: kullanmayın.** SA, boyanmış sürümün de aynı lisansla paylaşılmasını zorunlu kılar; NC ise ücretsiz yayında bile tek başına güvence vermez.
- **Özel lisans** ("ticari kullanım serbest" gibi): lisans metninin ekran görüntüsünü ve sayfa adresini teslim notuna koyun. "Yeniden dağıtma" yasağı varsa kaynak dosyayı herkese açık bir yere koymayın.
- Üstünü boyamak açıkça **uyarlamadır**: boyanan dosya kaynağın lisansına tabidir. "Bakarak sıfırdan çizdim" de lisanstan kendiliğinden çıkarmaz; emin değilseniz sorun.

---

## 7. Dosya adları ve teslim

### 7.1 Format
- **PNG, sRGB, 8 bit, saydamlıklı.** JPG kullanılmaz.
- Piksel art PNG'leri **1× ölçekte** teslim edin, büyütmeyin. (32 piksellik karakter 32 piksel olarak gelsin.)
- Kaynak dosya her zaman PNG'nin yanında gelir.

### 7.2 Adlandırma

Desen: **`tur_varlik_eylem_varyant.png`** (varyant isteğe bağlı)

- Yalnız **küçük harf, İngilizce karakter, rakam ve alt çizgi.** Türkçe karakter (ş, ğ, ı, ö, ü, ç), boşluk ve büyük harf yok.
- Animasyonlu dosyada eylem kısmı yazılmaz, tag'ler animasyon adını taşır: `char_player.aseprite`.

| Tür öneki | Anlamı | Örnek |
|---|---|---|
| `char_` | karakter, düşman, boss | `char_player.aseprite`, `char_boss_idle.png` |
| `prop_` | obje | `prop_hook_head.png` |
| `tile_` | tileset | `tile_cave_ground.png` |
| `bg_` | boyalı arka plan katmanı | `bg_cave_far_01.png`, `bg_cave_fg_02.png` |
| `fx_` | efekt | `fx_sword_slash.aseprite`, `fx_impact_spark.aseprite` |
| `ui_` | arayüz | `ui_health_full.png` |
| `portrait_` | diyalog portresi | `portrait_sword_angry.png` |

Kol ve düşman dosyalarının kesin adları aktarma aracı bitince bu tabloya eklenecek. *(geçici)*

### 7.3 Teslim klasörü

Ortak Drive klasörü: `teslim/<adiniz>/<YYYY-MM-DD>/`. İçine:
1. Export dosyaları (PNG).
2. Kaynak dosyaları (`.aseprite`, `.psd`, `.kra`).
3. Kısa bir teslim notu (`not.txt`): ne teslim edildi, hangi sürüm, vuruş kareleri, bilinen eksikler.
4. Kullandığınız hazır bir şey varsa (fırça hariç: doku, font, referans fotoğraf, üstünü boyadığınız yer tutucu), kaynağını ve lisansını nota yazın.

Teslimden sonra dosyayı biz oyuna alırız ve sahnede kontrol ederiz. Bir sorun varsa size bu belgedeki ilgili maddeyle döneriz.

### 7.4 Teslim öncesi kontrol listesi
- [ ] Dosya adı kurala uyuyor.
- [ ] Tag adları bu belgedekilerle birebir aynı, yönleri Forward.
- [ ] PNG, saydamlıklı, 1× ölçek.
- [ ] Piksel art'ta yarı saydam piksel yok.
- [ ] Animasyonda bütün kareler aynı tuval boyutunda, ayak noktası alt satırın ortasında.
- [ ] Oyuncu gövdesinde `socket_front` ve `socket_back` her karede tek piksel, katmanlar görünmez.
- [ ] Arka plan katmanları ayrı, en fazla 4096 piksel genişlikte.
- [ ] Siluet testi geçti.
- [ ] Kaynak dosya ve teslim notu eklendi; yer tutucu üstüne boyandıysa kaynağı ve lisansı notta.

---

## 8. Bölüm tasarımı kuralları

Bölüm tasarımcıları kâğıtta veya çizim programında çalışır. Tasarımı Unity'ye biz kurarız.

- **Kareli kâğıt kullanın: 1 kutu = 1 kare (16 piksel).**
- Ekran 30×17 kutu. Bir bölüm birden fazla ekran olabilir, kamera oyuncuyu takip eder.
- Oyuncu **2 kutu** boyunda.
- **Ölçüler (geçici, prototipten sonra kesinleşir):**
  - Kanca menzili: yaklaşık **8 kutu**.
  - Zıplama yüksekliği ve mesafesi: prototipten sonra verilecek.
- **Kanca bölümün omurgasıdır.** Kanca, kancalanabilir zemine ve duvara (`#`) saplanır; kancanın tutmadığı yüzeyleri `X` ile işaretleyin. Kılıç yalnız yakın dövüş içindir, hareket taşımaz. Oyuncunun nereden nereye uçacağına siz karar verirsiniz. Her önemli kanca geçişi için oyuncunun hangi yönden yaklaşıp hangi yöne fırlayacağını bir okla gösterin.

### İşaret dili

Kâğıttaki harfler, bölümü kuran aracın harfleriyle aynıdır; böylece çiziminiz doğrudan harita dosyasına aktarılır. Ayrıntı ve örnek harita: [bolum-kurma.md](bolum-kurma.md).

| İşaret | Anlamı |
|---|---|
| `#` | zemin/duvar, kancalanabilir |
| `X` | zemin/duvar, kancalanmaz (kanca geri döner) |
| `~` | çarpışmasız süs karosu |
| `^` | tehlike (diken) |
| `S` | oyuncu başlangıcı (bölümde tam bir tane) |
| `C` | checkpoint |
| `z` | dikene düşünce geri dönülen alan |
| `w` | düşman: Walker (böcek) |
| `f` | düşman: Flyer (sinek) |
| `t` | düşman: Spitter (tükürgen) |
| `b` | düşman: Brute (kalkanlı) |
| `k` | kasa |
| `d` | antrenman kuklası |
| `B` | boss arenası |
| `E` | çıkış (bölüm sonu) |

Kurucunun tanımadığı işaretler yalnız kâğıtta kalır, oyuna aktarılmaz:

| İşaret | Anlamı |
|---|---|
| `→` | kanca sonrası beklenen fırlama yönü |
| `=` | içinden alttan geçilebilen tek yönlü platform *(kurucuda henüz yok)* |
| `?` | sır/gizli alan *(kurucuda henüz yok)* |

Her sayfanın köşesine bölüm adını, sürümünü ve tarihi yazın. Fotoğrafını ya da taramasını `teslim/` klasörüne koyun.
