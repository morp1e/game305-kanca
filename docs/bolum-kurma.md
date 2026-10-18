# Bölüm kurma

1. Önce `Tools/Kanca/Gri Kutu Sahnesini Kur` menüsünü çalıştırın. Bu işlem prefab ve Tile asset’lerini üretir. Ardından yeni sahne açıp `Assets/_Project/Scenes/Levels/` altına kaydedin.
2. `GameObject > 2D Object > Tilemap > Rectangular` ile `Ground` Tilemap oluşturun. Tile Palette penceresini açın, `Assets/_Project/Art/Pixel/_Greybox/Tiles/tile_ground.asset` dosyasını palette sürükleyin ve zemin hücrelerini boyayın. Tilemap GameObject katmanını `Ground` yapın; `TilemapCollider2D` ile `Rigidbody2D` Static kullanın.
3. `Assets/_Project/Prefabs/` içinden `Anchor`, `Hazard_Spikes`, `Checkpoint` ve `PlayerRig` prefab’larını sahneye sürükleyin. Kanca noktalarını Anchor prefab’ıyla işaretleyin.
4. Denemek için önce `Tools/Kanca/Gri Kutu Sahnesini Kur` ile Sandbox sahnesini üretin ve Play’e basın. Yeni bölümünüzü denemek için sahneyi Build Settings’e ekleyin.
