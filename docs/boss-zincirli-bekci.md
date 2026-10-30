# Zincirli Bekçi: boss tasarımı

**Sürüm:** v0.1 (2026-10-30) · Gri kutu, iki faz oynanabilir. Sayılar başlangıçtır; hepsi `Data/Boss/` altındaki ScriptableObject'lerde, oynayarak ayarlanır.

## Neden bu boss

Kanca taşıyan oyunlarda en iyi boss sorusu "doğru anda çek"tir: boss'un bir parçası ancak belli bir anda çekilebilir, çekiş bir pencere açar, pencereyi ikinci araç doldurur. Sekiro'da Guardian Ape'in boyun yarası kancayla çekilir; Bionic Commando Rearmed'da kancayla atılan varil D-1 Beetle'ı devirip zayıf noktasını açar. Bizim kancanın üç modu var (yüzeye takılıp fırlama, hafif hedefi çekme, ağır hedefe çekilme) ve kılıç hareket taşımıyor. Zincirli Bekçi bu üçünü de tek dövüşte soruyor:

- **Hafif çekiş:** önündeki kalkan zincirle boss'a bağlı. Yalnız saldırı sonrası gevşer; o anda kancayla sökülür.
- **Yüzey + fırlama:** pogo olmadığı için yüksek boss'un üstünden geçmenin ve hücumdan kaçmanın yolu arenanın kirişleri.
- **Ağır çekiş:** boss gövdesine kancalanan oyuncu kendini ona çeker ve mesafeyi kapatır.

Tematik bağ: boss'un kalkan zinciri oyuncunun kancasının aynası. Konuşan kılıç bunu söyleyebilir.

## Döngü

1. Bekçi arenaya girilince uyanır, kapılar kapanır.
2. Faz 1: kalkan önden gelen kılıcı durdurur ("tink"). Oyuncu bir saldırının toparlanmasını bekler; kalkan gevşeyip sarıya döner.
3. Gevşek kalkan kancayla sökülür → Bekçi 1,6 s sendeler, her yönden hasar alır (kılıç aralığı 0,4 s ile ~4 vuruş). Sonra zincir kalkanı 0,5 s'de yuvasına geri çeker.
4. Arkaya geçip 2 s içinde 6 isabet de 1,0 s sendeletir (Hollow Knight'ın kombo eşiği kalıbı).
5. Can 10'un altına inince geçiş **sıraya alınır**, aktif saldırı bitince uygulanır (saldırının ortasında kesilmez): 2 s dokunulmaz kükreme, kalkan kırılır.
6. Faz 2: önden blok yok, saldırılar hızlanır, kırbaç ve kiriş çöküşü eklenir.
7. Can 0: kapılar açılır, 2,5 s sonra oyun biter (bitiş ekranı).

## Saldırılar

| Saldırı | Faz | Hazırlık | Etki | Toparlanma | Kalkan gevşer | Oyuncunun cevabı |
|---|---|---:|---|---:|---|---|
| Kalkan Hücumu | 1, 2 | 0,8 s | Duvara kadar 16 u/s koşu (oyuncudan hızlı), önde 1 hasar | 1,0 s, duvara gömülü | evet | Kirişe kancalan ya da duvar zıpla, üstünden geç; gömülünce kalkanı sök |
| Zemin Ezmesi | 1, 2 | 0,7 s | İki yöne zemin boyunca 1 karo yüksek şok dalgası, 10 u/s, 1 hasar | 0,6 s | evet | Zıpla ya da kancayla havalan; toparlanmada sök |
| Kalkan Darbesi | 1, 2 | 0,45 s | Yalnız oyuncu 2,5 u içindeyse; önde 2×2 u, 1 hasar | 0,35 s | hayır | Önünde oyalanma; arkaya geç |
| Zincir Kırbacı | 2 | 0,6 s | Arena boyu yatay şerit, sırayla alçak / yüksek, 1 hasar | 0,7 s | (kalkan yok) | Alçakta zıpla, yüksekte yerde kal |
| Kiriş Çöküşü | 2 | 0,8 s | Bir kiriş 3 s kanca tutmaz (kırmızı), altından 4 moloz düşer | 0,5 s | (kalkan yok) | Diğer kirişi veya duvarları kullan |

Hazırlık süreleri oyuncunun kaçış eylemine göre kuruldu: tepki 0,25 s + kanca uçuşu ve çekişin ilk karoları ≈ 0,43 s, üstüne pay → 0,6-0,8 s. Bu aralık Hollow Knight'ın Hornet (0,5-0,67 s) ve Great Nailsage Sly (0,75 s) değerleriyle örtüşüyor. Faz 2'de hazırlık × 0,8, taban 0,45 s. Saldırı seçimi ağırlıklı ve menzile duyarlı; aynı saldırı arka arkaya en çok iki kez.

## Sayılar

| Özellik | Başlangıç | Dayanak |
|---|---:|---|
| Boss gövdesi / kalkan | 3×3 u / 1×2,5 u | piksel sprite 48×48 hedefi |
| Can | 24 (kılıç 1 hasar) | HK'nin ilk boss'larının yaklaşık yarısı; öğretici tek boss |
| Faz eşiği | 10 can (~%42) | HK eşikleri %50 / %40 aralığında |
| Saldırılar arası | F1 0,6-1,0 s, F2 0,3-0,6 s | Ö |
| Sökme sendelemesi / kalkan dönüşü | 1,6 s / 0,5 s | Grimm 2-3,5 s, Dead Cells kanca sersemlemesi 1 s |
| Arka kombo | 2 s içinde 6 isabet → 1,0 s | HK kombo eşiği kalıbı |
| Faz geçişi / zafer gecikmesi | 2,0 s / 2,5 s | Ö |
| Oyuncuya hasar | her isabet 1 (oyuncu canı 5) | oyuncu hasar modeli |

## Arena

İç alan 28×12 karo. Arena kökü zemin üst yüzünün ortasındadır (y = 0); iç alan x ∈ [-14, 14), y ∈ [0, 12). Bölüm haritası zemini, iki yan duvarı ve tavanı çizer; duvarlarda zemin hizasında 3 karo yüksek kapı boşlukları bırakır (`Level_01.txt`'de `B` arena kökünü, iç alanın 15. sütununa konur).

```
y=12  ############################################   tavan
y=7          [ kiriş ]                 [ kiriş ]        alt yüzleri kancalanır (x -9..-5 ve 5..9)
y=1          giriş →                     Bekçi
y=0   ##[kapı]##############################[kapı]##   zemin
```

- Kirişler arena prefab'ının kendi objeleridir (Ground katmanı, 4×1 u), haritaya çizilmez. Bölüm zemini tek bir Tilemap collider'ı olduğu için kirişi haritaya çizip "kancaya kapat" demek bütün bölümü kapatırdı.
- Kiriş, zıplama tepesinden kancalanabilecek yükseklikte (zıplama 3,6 karo, kanca menzili 8). Kirişe kancalanan oyuncu boss'un 4 karo üstünden arkasına geçer.
- Merkez boş: hücum için düz koşu yolu.
- Giriş tetikleyicisi iç alanın 4 karo içinde; oyuncu geçince kapılar kapanır, `Event_BossFightStarted` boss çubuğunu açar. Oyuncu ölürse dövüş sıfırlanır: boss başlangıç konumu, can, faz ve kalkan; mermiler temizlenir, kapılar açılır.

## Teknik akış

```
Dormant → Intro → Idle → Select → Telegraph → Active → Recover → Idle
Kesenler: Staggered → ShieldRewind → Idle;  PhaseTransition → Idle;  Dead
```

- Mantık kodda (`BossBrain`, enum + switch), süreler SO'da (`BossDefinition`, `BossPhase`, `BossAttack`). Görsel yalnız duruma tepki verir; animasyon klibinin uzunluğu mantığı belirlemez.
- Kalkan kendi Kinematic Rigidbody2D'si olan hafif `HookPullable`'dır. Kanca takılınca (`Hooked`) yuvayı izlemeyi bırakır; kanca bırakınca (`Released`) söküldü sayılır ve yere düşer.
- Şok dalgası ve moloz her fizik adımında kendi kutusunu Ground ve Player'a karşı sorgular; Kinematic gövde ile statik zemin arasında trigger olayı üretilmediği için olaya güvenilmez.
- Kurulum: `Tools/Kanca/Boss'u Kur` (`Game.Editor.Boss.BossFactory.BuildAll`) ya da hepsi birden `Tools/Kanca/Tum Icerigi Kur`.

## Üretim planı

- **Ara jüri (~26 Kasım):** bu belge + gri kutu iki faz oynanabilir. Sprite yok, ses yok.
- **Final (~11 Ocak):** piksel animasyon (bekleme 4, yürüme 6, hücum 3+4+3, ezme 3+2+2, darbe 2+2+2, sendeleme 3, kükreme 6, kırbaç 3+2+2, ölüm 6-8 kare; kalkan ve zincir ayrı sprite; toplam ~55-60 kare), ses, kılıç replikleri, ayar.

## Kaynaklar

Araştırma: Gizmo, 2026-10-30 (Hollow Knight wiki sayısal tabloları, GDKeys "Anatomy of an Attack", Sekiro ve Bionic Commando Rearmed rehberleri, Dead Cells wiki, Cuphead geliştirici röportajı, iki kişilik bir jam ekibinin boss postmortem'i). Kod veya varlık kopyalanmadı; kalıplar anlaşılıp yeniden tasarlandı.
