# Ekip Rehberi

**Sürüm:** v0.1 (2026-10-18) · **Sahibi:** Özenç

Bu belge ekibin nasıl birlikte çalıştığını anlatır: kim ne yapar, iş nasıl akar, "bitti" ne demek. Herkes bir kez okur. Çizimciler ve bölüm tasarımcıları için asıl belge [sanat-standartlari.md](sanat-standartlari.md).

## 1. Roller

| Kişi | Rol | Araç |
|---|---|---|
| Özenç | Yön, kod, Unity entegrasyonu, repo sahibi | Unity, Git |
| Efe | Unity'de düzenleme, kod | Unity, Git |
| *(ad)* | Karakter ve piksel art | Aseprite, Drive |
| *(ad)* | Arka plan | Photoshop/Krita, Drive |
| *(ad)* | Bölüm tasarımı | kâğıt veya çizim programı, Drive |

Rolleri ilk toplantıda tamamlayın.

**Entegratör:** çizimleri ve bölüm tasarımlarını Unity'ye aktaran kişi Özenç'tir. Drive'a teslim edilen dosya, entegratör oyuna aldığında "girdi" sayılır.

## 2. Kurulum (Unity kullananlar için)

1. **Unity Hub** ve **Unity 6000.3.26f1**. Sürüm **tam olarak** bu olmalı. Farklı bir yama sürümüyle açılan proje dosyaları yeniden yazar ve gereksiz değişiklik üretir. Hub'da Installs > Install Editor > Archive yolundan bu sürümü seçin, modüllerden **Web Build Support**'u işaretleyin.
2. **Visual Studio 2022 Community**, "Game development with Unity" iş yüküyle.
3. **Git** ve **Git LFS.** Kurduktan sonra bir kez: `git lfs install`
4. Repoyu klonlayın. GitHub Desktop da olur.
5. **Unity Smart Merge** (sahne ve prefab çakışmaları için). Kullanıcı klasörünüzdeki `.gitconfig` dosyasına ekleyin:

```gitconfig
[merge]
    tool = unityyamlmerge
[mergetool "unityyamlmerge"]
    trustExitCode = false
    cmd = 'C:/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```

Çakışma çıkarsa `git mergetool` çalıştırın. Yol sizde farklıysa Hub > Installs içinden editörün klasörünü bulun.

6. Projeyi Hub'da **Add > Add project from disk** ile repo klasöründen açın. İlk açılış birkaç dakika sürer.

**Okul laboratuvarı:** laboratuvar bilgisayarlarında hangi Unity sürümü olduğu henüz bilinmiyor. Öğrenene kadar projeyi laboratuvarda **farklı bir sürümle açmayın.** Açılırsa Unity yükseltme isteyebilir, o değişiklikleri commit'lemeyin.

## 3. Repo kuralları

- **`main` her zaman açılabilir ve oynanabilir.** Doğrudan `main`'e yarım iş gönderilmez.
- İş başına kısa ömürlü dal: `feat/kanca-nisan`, `fix/boss-faz2`, `art/oyuncu-kosma`. Birleşince silinir.
- Commit mesajı: `tur(kapsam): ne değişti`
  - `feat(kanca): anchor kilitlenmesi eklendi`
  - `fix(oyuncu): duvara yapışma düzeltildi`
  - `art(arkaplan): magara far katmani`
  - `docs(gdd): boss bölümü`
- Commit'ten önce `git status` ve diff'e bakın. Özellikle ayar dosyalarında (SO) yanlışlıkla değişmiş değer var mı kontrol edin.
- Büyük dosyalar (PNG, PSD, ses) otomatik olarak Git LFS'e gider, ayrıca bir şey yapmanız gerekmez.

## 4. Sahne sahipliği

Unity'de en çok iş, iki kişinin aynı sahneyi aynı anda değiştirmesiyle kaybedilir.

| Sahne | Sahibi |
|---|---|
| `Title`, `End` | Özenç (koddan üretilir: `UiFactory`) |
| `Levels/Level_01` | Özenç (koddan üretilir: `Level_01.txt` haritası; elle düzenleme yeniden kurulumda kaybolur) |
| `Sandbox/Sandbox_Ozenc` | Özenç |
| `Sandbox/Sandbox_Efe` | Efe |

`Sandbox_Ozenc` sahnesi elle Tile Palette ile düzenlenen kişisel test bölümüdür; `Sandbox_Hook` otomatik üretilir.

Sahibi olmadığın bir sahneyi değiştireceksen önce sahibine yaz. Denemeleri kendi Sandbox sahnende yap. Prefab ve ayar dosyalarını (SO) değiştirmek serbesttir, ama aynı prefab üzerinde aynı anda iki kişi çalışmıyorsa.

## 5. Görev panosu

Tek bir GitHub Projects panosu. Sütunlar: **Backlog → Bu hafta → Yapılıyor → İncelemede → Bitti.**
- Her kartın tek sahibi olur.
- Bir kişi aynı anda en fazla **2** kartı "Yapılıyor"da tutar.
- Repoya erişimi olmayan üyeler için panoyu her hafta toplantıda birlikte güncelleriz.

## 6. "Bitti" ne demek

Bir iş ancak şu yedi madde tamamsa "Bitti" sütununa geçer:
1. Oyunun ana sahnesinde oynanabiliyor.
2. Unity konsolunda hata yok.
3. Yapan dışında biri denedi.
4. Kullanılan hazır içerik (font, ses, doku) lisans kaydına girdi.
5. `main`'e birleşti.
6. Build alındı ve açıldı.
7. GDD'deki ilgili bölüm güncellendi.

## 7. Haftalık ritim

Perşembe GAME305 dersinin etrafında tek toplantı:
- **15 dk demo:** geçen hafta biten her şey oyunda gösterilir.
- **10 dk geri bakış:** ne iyi gitti, ne takıldı.
- **20 dk planlama:** "Bu hafta" sütunu doldurulur, herkes kendi kartını seçer.

Hafta içindeki durum yazılı olarak grupta paylaşılır, ayrı toplantı yok.

## 8. Asset akışı

```
Çizimci / bölüm tasarımcısı
  → Drive: teslim/<ad>/<tarih>/  (PNG + kaynak dosya + not.txt)
  → Entegratör Unity'ye alır, sahnede kontrol eder
  → Sorun varsa: sanat-standartlari.md'deki maddeyle geri bildirim
  → Sorun yoksa: commit, kart "Bitti"ye
```

## 9. Takvim

| Hafta | Tarih (yaklaşık) | Hedef |
|---|---|---|
| 2-3 | Ekim | Repo, standartlar, hareket ve kanca prototipi |
| 4-7 | Ekim-Kasım | Pitch, GDD, kanca hissinin oturması, ilk sanat |
| **8** | **~26 Kasım** | **Ara jüri: pitch + dokümantasyon** |
| 9 | Aralık | Mekanik ve sistemler |
| 10 | Aralık | Anlatı (konuşan kılıç) |
| 11 | Aralık | Sanat entegrasyonu |
| 12 | Aralık | Ses ve müzik |
| 13-14 | Aralık-Ocak | UI ve test |
| **15** | **~Ocak** | **Final jüri ve teslim** |

Teslimler hem basılı hem dijital yapılır. Geç teslim cezası 1-2 gün %10, 2-7 gün %20, bir haftadan sonra kabul yok.
