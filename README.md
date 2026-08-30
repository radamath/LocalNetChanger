# LocalNetChanger

Windows sistem tepsisinde çalışan, açık kaynak ağ profili yöneticisi. Kablolu ve kablosuz bağlantılar için statik IP profilleri oluşturup tek tıkla uygulayabilirsiniz.

**Lisans:** [GNU General Public License v3.0](LICENSE)

## Özellikler

- Sistem tepsisinde arka planda çalışır
- Sağ tık menüsü: **Ayarlar**, **Ağ Değiştir** (Kablolu / Kablosuz)
- Kontrol paneli: profil yönetimi ve uygulama ayarları
- Türkçe / İngilizce arayüz (sistem diline göre veya manuel seçim)
- Windows başlangıcında çalıştırma seçeneği
- Tek örnek (zaten çalışıyorsa yeni pencere açmaz)
- Harici NuGet paketi gerektirmez (.NET 8 WinForms)

## Gereksinimler

- Windows 10 / 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (kurulum paketi için)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (kaynak koddan derlemek için)
- IP değiştirmek için yönetici onayı (`netsh` UAC)

## Kurulum paketi oluşturma

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

Çıktılar `dist\` klasöründe oluşur (MSI ve isteğe bağlı Inno Setup EXE).

## Kaynak koddan derleme

```powershell
dotnet build LocalNetChanger.sln -c Release
```

## Çalıştırma

```powershell
dotnet run --project LocalNetChanger\LocalNetChanger.csproj
```

Komut satırı:

| Argüman | Açıklama |
|---------|----------|
| `--open-settings` | Kontrol panelini aç |
| `--tray-only` | Yalnızca tray (başlangıç kısayolu) |

Profiller `%APPDATA%\LocalNetChanger\profiles.json` dosyasında saklanır.

## Katkı

Pull request ve issue’lar memnuniyetle karşılanır. Değişiklikler GPL v3 kapsamında kalır.

## Lisans

Bu proje GPL v3 altında dağıtılır. Ayrıntılar için [LICENSE](LICENSE) ve [installer/license.txt](installer/license.txt) dosyalarına bakın.
