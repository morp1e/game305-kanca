# Metin haritadan bölüm kurma

Bölüm düzeni `Assets/_Project/Data/Levels/*.txt` dosyasındadır. Bir karakter bir karo hücresidir. İlk sütun x=0, en alttaki harita satırı y=0'dır. Satırları yukarıdan aşağı yazın; satır uzunlukları farklı olabilir, kısa satırların sağı boş kabul edilir. Boşluk ve `.` boş hücredir. `;` ile başlayan satırlar açıklamadır. Haritanın başındaki `@anahtar değer` satırları bölüm ayarıdır.

## Lejant

Lejant `Assets/_Project/Data/Levels/LevelLegend.asset` içinde tutulur. Asset yoksa kurucu varsayılan girdilerle oluşturur; mevcut lejantı değiştirmez.

| Karakter | Üretilen |
|---|---|
| `#` | Kancalanabilir Ground karosu. Üst yüzeyde 0/1/2, altında 20 numaralı karo kullanılır. |
| `X` | `NonHookable` işaretli Tilemap karosu. |
| `~` | Çarpışmasız dekor karosu. |
| `^` | `Hazard_Spikes` prefabı. |
| `S` | Oyuncu başlangıcı. Haritada tam bir tane olmalıdır. |
| `C` | Checkpoint prefabı. |
| `z` | HazardRespawnZone varsa trigger alanı, yoksa yer tutucu ve uyarı. |
| `w`, `f`, `t`, `b` | Sırasıyla Walker (böcek), Flyer (sinek), Spitter (tükürgen) ve Brute (kalkanlı) düşman prefab'ları. Eksik prefab uyarı ve yer tutucu üretir. |
| `k`, `d`, `B` | Kasa, TrainingDummy ve boss arena prefablari. |
| `E` | LevelExit prefabı varsa bölüm sonu. |

Bilinmeyen karakterler hata oluşturmaz; satır ve sütun numarasıyla uyarı verir. `@killHeight` oyuncunun düşme ölüm sınırını, `@cameraPadding` kamera çokgeni çevresindeki payı belirler.

## Örnek

```text
@name Level_01
@killHeight -6
@cameraPadding 1
; Basit başlangıç ve zemin
  C
S###
####
```

## Yeniden üretme

Unity'de `Level_01.txt` seçiliyken **Tools > Kanca > Bolumu Haritadan Kur** menüsünü çalıştırın. Seçili TextAsset yoksa `Level_01.txt` kullanılır. Batchmode girişi `Game.Editor.Level.LevelBuilder.BuildLevel01` metodudur. Sahne `Assets/_Project/Scenes/Levels/Level_01.unity` yoluna yazılır. Aynı harita tekrar kurulduğunda aynı Tilemap ve nesne dizilimi oluşur.

`Level_01.txt` altı gri kutu öğretim bölgesi içerir: hareket ve kılıç, kanca boşluğu, ilk düşmanlar, diken geçidi, dikey tırmanış ve boss öncesi arena. Harita değeridir; tasarım değişikliği için kod düzenlemek gerekmez.

## Elle test bölümü

`Tools/Kanca/Ozenc Sandbox Kur` menüsü `Sandbox_Ozenc` kişisel sahnesini ve Kenney Tile Palette'ini kurar. Sahneyi açın, ardından **Window > 2D > Tile Palette** penceresini açın ve `Palette_Kenney` paletini seçin. Palette'te `Ground`, `NonHookable` veya `Decor` Tilemap'i aktif hedef olarak seçip boyayın. `NonHookable` katmanına boyanan karolar kancayı geri döndürür. Kenney karoları geçici CC0 yer tutuculardır: 18 px hücrelidir, son oyunda kendi 16 px çizimlerimizle değiştirilecektir.


Düşman lejantı: `Walker` kenarda dönen yakın dövüş devriyesidir; telegraph sırasında kancayla kesilebilir. `Flyer` havada süzülür ve oyuncunun konumuna dalar. `Spitter` görüş hattında mermi atar; mermisi kılıçla sektirilebilir. `Brute` ağırdır, önündeki kalkan kılıcı engeller ve arkadan ya da toparlanırken vurulabilir.
