# Gameplay Toast Service - Architecture

## Amac

Kisa sureli gameplay bildirimleri tek bir bounded FIFO kuyrugundan sirali bicimde sunulur.
Servis yalniz tasima ve siralama altyapisidir; hangi gameplay olayinin toast uretecegine karar
vermez.

## Runtime Sozlesmesi

- `GameplayToastService`, en fazla `8` bekleyen mesaji tutar.
- Mesajlar geldikleri sirayla tuketilir; kapasite doluyken yeni talep fail-closed reddedilir.
- Bos metin reddedilir; sure `0.8 - 6.0` saniye araligina clamp edilir.
- Tonlar `Primary`, `Secondary`, `Warning` ve `Critical` olarak sunum metadata'si tasir.
- `GameplayHUDToolkitUI.GameFlow.cs`, mevcut `primaryToast` ve `secondaryToast` elementlerinde
  kuyrugu `Time.unscaledTime` ile oynatir. Bu nedenle Council/pause sirasinda aktif toast
  sunumu kilitlenmez.
- HUD kapandiginda aktif sunum ve bekleyen kuyruk temizlenir; yeni kosuya stale mesaj sizmaz.

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

- `GameplayToastServiceTests`: FIFO, ton/sure korunumu, capacity, bos metin ve duration clamp.
- `GameplayActionPresentationUtilityTests`: exact kaynak acigi, worker/capacity onceligi, meta
  currency acigi, Ingilizce research failure cevirisi ve Game Over effect progression copy'si.
- `GameplayHUDToolkitContractTests`: bounded queue, warning tone, aciklanabilir action state ve
  raw internal mesajlarin player-facing yuzeylere sizmamasi kontrati.
- `WorkerAllocationPlayModeTests`: hem UI Toolkit Barracks hem legacy `MarketUI` Basic Archer
  dugmesinin bir Wood eksiginde aktif kalmasi ve exact Ingilizce warning toast'i.
- Canli Game View kontrolu: toast stack raycast almaz, HUD kontrollerini kapatmaz ve pause'da
  unscaled sunumunu surdurur.
