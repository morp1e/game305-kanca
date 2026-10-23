# Bölüm kurma

1. Önce `Tools/Kanca/Gri Kutu Sahnesini Kur` menüsünü çalıştırın. Bu işlem prefab ve Tile asset’lerini üretir. Ardından yeni sahne açıp `Assets/_Project/Scenes/Levels/` altına kaydedin.
2. `GameObject > 2D Object > Tilemap > Rectangular` ile `Ground` Tilemap oluşturun. Tile Palette penceresini açın, `Assets/_Project/Art/Pixel/_Greybox/Tiles/tile_ground.asset` dosyasını palette sürükleyin ve zemin hücrelerini boyayın. Tilemap GameObject katmanını `Ground` yapın; `TilemapCollider2D` ile `Rigidbody2D` Static kullanın.
3. `Assets/_Project/Prefabs/` içinden `Anchor`, `Hazard_Spikes`, `Checkpoint` ve `PlayerRig` prefab’larını sahneye sürükleyin. Kanca noktalarını Anchor prefab’ıyla işaretleyin.
4. Denemek için önce `Tools/Kanca/Gri Kutu Sahnesini Kur` ile Sandbox sahnesini üretin ve Play’e basın. Yeni bölümünüzü denemek için sahneyi Build Settings’e ekleyin.

`TrainingDummy` kılıç ve hafif kanca denemeleri için Enemy katmanına, `HeavyDummy` oyuncuyu çeken ağır hedef olarak Enemy katmanına, `Crate` hafif ve hasar almayan kanca hedefi olarak Default katmanına yerleştirilir.

## Elle test bölümü

`Tools/Kanca/Ozenc Sandbox Kur` menüsü `Sandbox_Ozenc` kişisel sahnesini ve Kenney Tile Palette'ini kurar. Sahneyi açın, ardından **Window > 2D > Tile Palette** penceresini açın ve `Palette_Kenney` paletini seçin. Palette'te `Ground`, `NonHookable` veya `Decor` Tilemap'i aktif hedef olarak seçip boyayın. `NonHookable` katmanına boyanan karolar kancayı geri döndürür. Kenney karoları geçici CC0 yer tutuculardır: 18 px hücrelidir, son oyunda kendi 16 px çizimlerimizle değiştirilecektir.
