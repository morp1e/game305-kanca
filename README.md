# game305-kanca

GAME305 Project and Portfolio I grup projesi: sonunda boss dövüşü olan bir 2D platform oyunu. Sağ elde konuşan bir kılıç (yakın dövüş), sol elde bir kanca: kanca zemine ve yüzeylere takılır, oyuncu çekilip ivmeyle fırlar; hafif düşmanı oyuncuya çeker, ağır olana oyuncuyu çeker. Bölüm, kalkanı ancak kancayla sökülebilen bir boss'la (Zincirli Bekçi) biter.

- **Motor:** Unity 6000.3.26f1 (6.3 LTS), URP 2D
- **Kontrol:** klavye ve fare
- **Görsel:** piksel art karakter, elle boyanmış katmanlı arka plan
- **Hedef platform:** Windows ve tarayıcı (itch.io)

## Belgeler

| Belge | Kimin için |
|---|---|
| [Sanat ve bölüm tasarımı standartları](docs/sanat-standartlari.md) | çizimciler, bölüm tasarımcıları |
| [Ekip rehberi](docs/ekip-rehberi.md) | herkes |
| [Teknik mimari](docs/teknik-mimari.md) | kod yazanlar |
| [Bölüm kurma](docs/bolum-kurma.md) | bölüm tasarımcıları, entegratör |
| [Boss: Zincirli Bekçi](docs/boss-zincirli-bekci.md) | herkes, ara jüri |
| [Jüri teslimleri ve yasal](docs/teslim-ve-yasal.md) | herkes, ara jüri öncesi |

## Başlarken

Unity kullanacaksan önce [Ekip rehberi › Kurulum](docs/ekip-rehberi.md#2-kurulum-unity-kullananlar-için) bölümünü uygula. Unity sürümü tam olarak **6000.3.26f1** olmalı.

## Oyunu açmak

Oyun `Title` sahnesinden başlar (Başla → `Level_01` → boss → bitiş ekranı). Her sahne tek başına da Play'e basılabilir.

Prefab'lar, arayüz, menü sahneleri ve `Level_01` koddan üretilir. Kodu veya bölüm haritasını (`Assets/_Project/Data/Levels/Level_01.txt`) değiştirdikten sonra Unity'de **Tools > Kanca > Tum Icerigi Kur** çalıştırın. Var olan ayar değerleri ezilmez.

| Tuş | Eylem |
|---|---|
| A / D | yürü |
| Space veya W | zıpla (basılı tutmak daha yüksek) |
| Sağ tık | kanca at; tekrar basınca geri çağır |
| Sol tık | kılıç |
| Esc | duraklat |
| R | son checkpoint'e dön (geliştirme) |
| F1 | hata ayıklama paneli (geliştirme) |

Build: **Tools > Kanca > Build > Windows** (`Builds/Windows/Kanca.exe`) veya **WebGL**.
