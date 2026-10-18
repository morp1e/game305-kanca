# game305-kanca: ajan talimatları

GAME305 grup projesi, Unity 6000.3.26f1 (6.3 LTS), URP 2D. Kanonik tasarım `docs/` altındadır; bir kararı değiştirmeden önce ilgili belgeyi oku ve aynı değişiklikte güncelle.

- Editör sürümü sabittir: 6000.3.26f1. Paket veya editör yükseltmesi Özenç'in kararıdır.
- İlke: kod davranışı tanımlar, ayarlar ScriptableObject'te, içerik prefab/Tilemap/asset'tedir. Koda sayı gömme.
- Proje kodu `Assets/_Project/` altında; namespace `Game.<Alan>`; dosya başına bir sınıf; dosya ve klasör adları ASCII, boşluksuz.
- Girdi `Update`'te okunur, `FixedUpdate`'te tüketilir. Oyuncu Kinematic Rigidbody2D + `Slide`/`Cast` ile hareket eder.
- Sahne sahipliği `docs/ekip-rehberi.md` §4'tedir. Başkasının sahnesini değiştirme; denemeyi Sandbox sahnesinde yap.
- Unity YAML dosyalarını (`.unity`, `.prefab`, `.asset`) elle düzenlemekten kaçın; mümkünse editör veya editör script'iyle değiştir.
- Commit biçimi: `tur(kapsam): açıklama` (feat, fix, art, docs, chore, test).
- Belgelerde ve kullanıcıya görünen metinde uzun tire kullanılmaz.
