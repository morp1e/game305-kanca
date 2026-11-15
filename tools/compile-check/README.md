# compile-check

Unity'yi açmadan C# derleme kontrolü. Unity açıkken, ayrı bir worktree'de veya CI'da kullanılır.

```
dotnet build tools/compile-check/All.csproj -nologo -v q
```

- `Runtime.csproj` yalnız `Assets/_Project/Scripts/Runtime` kodunu UnityEditor **olmadan** derler. Oyun koduna editör API'si sızarsa burada hata verir.
- `All.csproj` Runtime'a ek olarak editör kodunu ve testleri derler.
- Paket DLL'leri (`Input System`, `Cinemachine`, URP) `Library/ScriptAssemblies` altından gelir. Bu klasör proje Unity'de bir kez açılınca oluşur. Başka bir kopyadan (worktree) derlerken ana kopyanın klasörü verilir:
  `-p:ScriptAssembliesDir=<ana>/Library/ScriptAssemblies -p:PackageCacheDir=<ana>/Library/PackageCache`
- Unity yolu `UnityVersion` (varsayılan 6000.3.28f1) ve işletim sisteminden bulunur: Windows `Editor\Data`, macOS `Unity.app/Contents/Resources/Scripting`. Farklı kurulumda `-p:UnityEditorDir=...` verilir.
- .NET Framework referans paketi bir kez internetle indirilir (`~/.nuget/packages`); sonra derleme ağsız çalışır, ağı kapalı Codex sandbox'ı dahil.
- Bu kontrol Unity'nin derlemesinin yerini tutmaz: asmdef kuralları ve serileştirme Unity'de doğrulanır. Amaç hızlı geri bildirim.
