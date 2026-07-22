# Gameplay Toast Service - Architecture

## Amac

Kisa sureli gameplay bildirimleri bounded bir kuyruktan, ayni anda en fazla uc kartlik gecici
bir action-feedback stack'ine tasinir. Servis yalniz tasima ve siralama altyapisidir; hangi
gameplay olayinin toast uretecegine karar vermez.

## Runtime Sozlesmesi

- `GameplayToastService`, en fazla `8` bekleyen mesaji tutar.
- Mesajlar geldikleri sirayla tuketilir; kapasite doluyken yeni talep fail-closed reddedilir.
- Bos metin reddedilir; sure `0.8 - 6.0` saniye araligina clamp edilir.
- Tonlar `Primary`, `Secondary`, `Warning` ve `Critical` olarak sunum metadata'si tasir.
- `GameplayHUDToolkitUI.GameFlow.cs`, kuyrugu dinamik `toastStack` icinde oynatir. Ayni anda en
  fazla `3` kart gorunur; yeni kart alta eklenir ve onceki kartlar yukariya kayar.
- Her oyuncu tiklamasi ayri bir karttir. Ayni metin art arda gelse bile birlestirilmez veya mevcut
  kartin suresi yenilenmez.
- Dorduncu kart geldiginde en eski gorunen kart kaldirilir. Boylece en yeni player action'i aninda
  gorunur kalirken stack ekrani kaplayacak sekilde buyumez.
- Varsayilan sunum `2.4` saniye, warning sunumu `3.2` saniyedir. Sure doldugunda kart `180 ms`
  exit transition'iyle kaldirilir.
- Omur ve animasyon takibi `Time.unscaledTime` kullanir. Bu nedenle Council/pause sirasinda aktif
  toast sunumu kilitlenmez.
- Toast kartlari pointer/raycast almaz; alttaki HUD kontrollerini engellemez.
- HUD kapandiginda aktif sunum ve bekleyen kuyruk temizlenir; yeni kosuya stale mesaj sizmaz.

## UI Button Audio Sozlesmesi

UI Toolkit document root'u `ClickEvent` dinler, gercek hedefin bir `Button` oldugunu dogrular ve
merkezi `UiSoundFeedback.PlayClick()` yolunu cagirir. Boylece Toolkit dugmeleri legacy uGUI
raycast kontrolune bagli kalmaz. Ses, `DeadWallsAudioProfileSO.UiClickClip` ve kullanicinin SFX
seviyesi uzerinden oynatilir; tekil callback'ler ikinci kez click sesi calmaz.

## Onayli Eylem Hatasi Kapsami

Owner, oyuncunun bilincli olarak bastigi bir eylem dugmesi reddedildiginde exact nedenin toast
olarak gosterilmesini onaylamistir. Aktif kapsam:

- Economy bina ve housing satin alimlari,
- Archer recruit/retrain,
- Arrow refill ve run-ici supply upgrade,
- War Doctrine ve Castle Heart research,
- Game Over kalici meta satin alimlari.

Kaynak yetersizliginde toast tam eksigi `NEED 14 MORE WOOD` gibi acik kaynak adlariyla verir.
Worker, garrison capacity, kilitli archer tipi, dolu arrow reserve, prerequisite, maximum level ve
durable meta save engelleri ayri nedenlerdir. `GameplayActionFeedbackUtility` karar sahibi degildir;
canli `GameManager`, quote/evaluation ve katalog owner'larindan alinan snapshot'i player-facing
metne cevirir.

Oyuncuya gorunen toast metni daima Ingilizcedir. Runtime servislerinin internal hata mesaji veya
`reason` alani dogrudan toast'a tasinmaz; War Doctrine ve Castle Heart dahil butun action-failure
yollari `GameplayActionFeedbackUtility` icindeki Ingilizce presentation copy'sinden gecer.

`NewGameScene` gecis doneminde hem UI Toolkit Barracks hem legacy `MarketUI` Archer callback'ini
barindirir. Iki yuzey de kaynak veya worker eksigi gibi aciklanabilir bir reddi disabled duruma
cevirmeden ayni warning toast'i uretir. Tech-locked ve garrison-max gibi terminal buton durumlari
disabled kalabilir.

Faz degisimi, pasif kaynak dolmasi, savunma alarmi veya event sonucu gibi oyuncunun tiklamadigi
otomatik olaylar bu onayin disindadir ve yeni owner karari olmadan toast uretemez.

## Test Sahipligi

- `GameplayToastServiceTests`: FIFO, tekrar eden mesajlarin ayri action olarak korunmasi,
  ton/sure, capacity, bos metin ve duration clamp.
- `GameplayActionPresentationUtilityTests`: exact kaynak acigi, worker/capacity onceligi, meta
  currency acigi, Ingilizce research failure cevirisi ve Game Over effect progression copy'si.
- `GameplayHUDToolkitContractTests`: uc kartlik dinamik stack, lifecycle/exit class'lari, merkezi
  UI Toolkit click audio route'u, warning tone, aciklanabilir action state ve raw internal
  mesajlarin player-facing yuzeylere sizmamasi kontrati.
- `WorkerAllocationPlayModeTests`: hem UI Toolkit Barracks hem legacy `MarketUI` Basic Archer
  dugmesinin bir Wood eksiginde aktif kalmasi; tekrarlanan Toolkit tiklamalarinin uc ayri gorunen
  karta donusmesi, kartlarin otomatik kaybolmasi ve click sesinin gercek AudioSource'a ulasmasi.
