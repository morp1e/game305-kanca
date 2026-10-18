# compile-check

Unity'yi açmadan C# derleme kontrolü. Unity açıkken, ayrı bir worktree'de veya CI'da kullanılır.

```
dotnet build tools/compile-check/All.csproj -nologo -v q
```

- `Runtime.csproj` yalnız `Assets/_Project/Scripts/Runtime` kodunu UnityEditor **olmadan** derler. Oyun koduna editör API'si sızarsa burada hata verir.
- `All.csproj` Runtime'a ek olarak editör kodunu ve testleri derler.
- Paket DLL'leri (`Input System`, `Cinemachine`, URP) `Library/ScriptAssemblies` altından gelir. Bu klasör proje Unity'de bir kez açılınca oluşur. Başka bir kopyadan (worktree) derlerken ana kopyanın klasörü verilir:
  `-p:ScriptAssembliesDir=<ana>/Library/ScriptAssemblies -p:PackageCacheDir=<ana>/Library/PackageCache`
- Bu kontrol Unity'nin derlemesinin yerini tutmaz: asmdef kuralları ve serileştirme Unity'de doğrulanır. Amaç hızlı geri bildirim.
