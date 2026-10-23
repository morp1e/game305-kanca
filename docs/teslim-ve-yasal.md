# Jüri Teslimleri ve Yasal Dokümantasyon

**Sürüm:** v0.1 (2026-10-18) · Hukuki tavsiye değildir, kaynakların ne dediğinin özetidir.

## 1. Ders ne bekliyor

GAME305 syllabus'una göre:
- **Ara jüri (8. hafta)** iki şeyi ölçüyor: oyun dokümantasyonu hazırlama ve pitch.
- **Final (15. hafta)** projenin tamamlanmasını ve **platforma gönderilmesini** kapsıyor: "Students will work on these projects until completion and submission to these platforms." Bu yüzden yasal dokümantasyon bölümü pratikte bir itch.io yayın hazırlığıdır.
- Not: ara jüri %40, final %60. Teslim basılı ve dijital.
- Syllabus'ta jüri rubriği yok. **Hocaya sorulacak:** değerlendirme ölçütleri ve finaldeki "submission"ın canlı itch.io yayını olup olmadığı.

## 2. Ara jüri paketi

### GDD (yaklaşık 10 sayfa, basılı)
1. **Tek sayfalık tasarım** (Stone Librande'nin "one-page design" yöntemi): çekirdek döngü, kılıç-kanca mekaniği ve boss arenası, bir bakışta okunan tek sayfalık diyagramla. Jürinin ilk okuyacağı sayfa.
2. **Konsept** (Tim Ryan'ın yapısı): tek cümlelik hook, oyuncunun gözünden anlatım, 4-6 ana özellik, tür ve platform, konsept çizimleri.
3. **Fonksiyonel tasarım (hafif):** hareket, kanca, savaş, boss fazları, UI, sanat yönü, ses, konuşan kılıç ve hikâye, bölüm akışı. Bu başlıklar 9-14. haftaların konularıyla birebir eşleşir, final dokümanı bu iskeletten büyür.
4. **Kapsam ve takvim:** 9-15. hafta hedefleri, rol tablosu, riskler ve "zaman yetmezse kesilecekler" listesi.
5. **Platform ve yasal** (aşağıda bölüm 3).
6. **Kaynakça.**

### Pitch (8-12 slayt)
1. Başlık ve hook
2. Tür ve platform
3. Çekirdek döngü
4. Ayırt edici özellik: konuşan kılıç-kanca
5. Boss dövüşü
6. Görsel hedef (moodboard)
7. Ses ve ton
8. Ekip ve roller
9. Kapsam, takvim, riskler
10. Platform ve yasal özet
11. Kapanış ve demo

Yayıncılara yapılan pitch'lerdeki bütçe slaytının yerine burada kapsam ve takvim geliyor.

## 3. Platform ve yasal bölüm

### 3.1 Platform: itch.io
- Ücretsiz yayın. Yayıncı hesabı **18 yaş üstü** bir üyede açılır.
- İçeriğin sahipliği ekipte kalır, itch.io yalnız münhasır olmayan bir dağıtım lisansı alır.
- Üretken yapay zekâ kullanıldıysa itch.io'da **etiketlenmeli** ve proje açıklamasında belirtilmelidir.
- Steam yalnız "ileride" notu: Steam Direct ücreti ürün başına 100 USD, içerik anketi zorunlu, Almanya'da yaş derecelendirmesi olmayan oyun görünmüyor.

### 3.2 Motor lisansı
- **Unity Student planında öğrenciler arası ortak proje yalnız eğitim amaçlı ve ticari olmayan kullanım içindir.** Oyun ticari olarak yayınlanacaksa (ücretli satış, bağış toplama vb.) ekipteki herkes **Unity Personal** kullanmalı. Personal, 200.000 USD gelir ve fonlamanın altında ücretsizdir. Unity 6'da açılış ekranı (splash) isteğe bağlı.
- Ücretsiz itch.io yayını için Student plan yeterli görünüyor. Ticari yayın kararı verilirse bu madde yeniden açılır.

### 3.3 Ekip katkı ve fikri mülkiyet sözleşmesi (1 sayfa, herkes imzalar)
Dayanak: ARUCAD Fikri ve Sınai Mülkiyet Hakları Yönergesi.
- Öğrenci işinin telif hakkı öğrenciye aittir.
- **Ortak projede sahiplik katkı oranına göre belirlenir.**
- Ticari kullanım öğrencinin yazılı onayını ister. Platformda ticari yayın olursa haklar ayrı bir sözleşmeyle düzenlenir.
- Üniversite işi eğitim, tanıtım ve arşiv amacıyla kullanabilir.

Sözleşmede bulunacaklar (bizim önerimiz):
1. Üye ve rol listesi.
2. Katkı oranının nasıl belirleneceği.
3. Herkesin kendi katkısını portfolyosunda kullanma hakkı.
4. Üniversitenin eğitim ve tanıtım kullanım hakkının bilindiği.
5. Ticari yayın kararının oybirliğiyle alınacağı ve o noktada ayrı sözleşme yapılacağı.
6. Bir üye ekipten ayrılırsa katkısının durumu.
7. Kredi listesi (oyunun sonunda kim ne yaptı).
8. Yapay zekâ araçlarının kullanım kuralı.

### 3.4 Üçüncü taraf içerik lisans kaydı
Kullanılan her hazır içerik (font, ses, müzik, doku, Asset Store paketi) için bir satır:

| Asset | Tür | Üretici ve kaynak URL | Lisans ve sürüm | Edinme tarihi | İzin ve sınırlar | Atıf metni | Değişiklik | Projedeki yol | Kanıt | Kontrol eden |
|---|---|---|---|---|---|---|---|---|---|---|

Unity Asset Store içeriği yalnız esaslı özgün içerikle birlikte, ürünün esaslı kısmını oluşturmayacak biçimde kullanılabilir. Tek başına dağıtılamaz.

Kullanılan içerik: Kenney, Pixel Platformer pack, CC0 1.0 Universal. Atıf zorunlu değildir; nezaket kredisi olarak Kenney'e teşekkür ederiz. Dosyalar: `Assets/ThirdParty/Kenney_PixelPlatformer/`.

### 3.5 Yaş ve içerik tanımı
IARC derecelendirmesi yalnız katılımcı mağazalar üzerinden alınabiliyor. itch.io için zorunluluk bulunamadı. GDD'de basit bir içerik tanımı yer alır: şiddet seviyesi, ışık parlaması ve yanıp sönme uyarısı vb.

### 3.6 Gizlilik ve EULA
- Oyun kişisel veri toplamıyorsa bu açıkça yazılır.
- Analitik ya da çevrimiçi özellik eklenirse gizlilik politikası ve kullanıcı onayı gerekir.
- itch.io için oyuna özel EULA zorunluluğu bulunamadı.

## 4. Kaynaklar
- GAME305 syllabus, 2026-2027 Güz (okul reposu)
- unity.com/legal/editor-terms-of-service/software (Student plan)
- unity.com/products/pricing-updates (Personal)
- unity.com/legal/as-terms (Asset Store EULA)
- itch.io/docs/legal/terms, itch.io/docs/creators/quality-guidelines
- partner.steamgames.com/steamdirect, partner.steamgames.com/doc/gettingstarted/contentsurvey/germany
- globalratings.com/faq (IARC)
- ARUCAD Fikri ve Sınai Mülkiyet Hakları Yönergesi, md. 8-10
- Stone Librande, "One-Page Designs", GDC 2010
- Tim Ryan, "The Anatomy of a Design Document", Game Developer, 1999
