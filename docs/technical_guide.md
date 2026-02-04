# Txuribeltz - Dokumentazio Teknikoa

## Aurkibidea
1. [Datu Iraunkorren Egitura](#datu-iraunkorren-egitura)
2. [Funtzionaltasun Tekniko Garrantzitsuak](#funtzionaltasun-tekniko-garrantzitsuak)
3. [Metodo Garrantzitsuen Dokumentazioa](#metodoak--funtzio-garrantzitsuak-dokumentatzea)
4. [Errore Ezagunak eta Mugak](#errore-ezagunak-eta-mugak)

---

## Datu Iraunkorren Egitura

### Erakundeen eta Harremanen Diagrama

```
┌─────────────────────────┐
│    ERABILTZAILEAK       │
│─────────────────────────│
│ PK: id (SERIAL)         │
│ username (VARCHAR)      │
│ password (VARCHAR)      │
│ elo (INTEGER)           │
│ mota (VARCHAR)          │
└───────────┬─────────────┘
            │
            │ 1
            │
            │ N
┌───────────▼─────────────┐
│       PARTIDAK          │
│─────────────────────────│
│ PK: id (SERIAL)         │
│ FK: player1_id ─────────┼─┐
│ FK: player2_id ─────────┼─┤
│ FK: winner_id ──────────┼─┘
│ played_at (TIMESTAMP)   │
└─────────────────────────┘
```

### Erakundeak/Taulak

#### Taula: **erabiltzaileak**
**Helburua**: Sistemako erabiltzaile guztien informazioa gordetzea (admin eta arrunteak). Autentikazioa, ranking-a eta erabiltzaile-kudeaketa egiteko erabiltzen da.

**Domeinua**: Erabiltzaile bat jokalari bat edo administratzaile bat izan daiteke. Bakoitzak bere ELO puntuazioa du partidetan arrakasta izan duen arabera.
```bash
| Zutabea    | Mota          | Deskribapena                                                |
|------------|---------------|-------------------------------------------------------------|
| `id`       | SERIAL (PK)   | Erabiltzailearen identifikatzaile bakarra                   |
| `username` | VARCHAR(50)   | Erabiltzaile-izena (login-erako, bakarra izan behar du)     |
| `password` | VARCHAR(255)  | Pasahitza testu arruntean (ez da enkriptatuta)              |
| `elo`      | INTEGER       | Puntuazioa ranking-erako (hasiera: 1000)                    |
| `mota`     | VARCHAR(10)   | Erabiltzaile mota: `admin` edo `user`                       |
```
**Kontsulta Adibidea**:
```sql
SELECT username, elo FROM erabiltzaileak ORDER BY elo DESC LIMIT 10;
```

---

#### Taula: **partidak**
**Helburua**: Jokatutako partiden historikoa gordetzea estatistikak kalkulatzeko (irabaziak, galduak, winrate).

**Domeinua**: Partida bat bi jokalari arteko lehia bat da non bat irabazle irteten den. Partida bakoitza datu-basean gordetzen da amaitzean.
```bash
| Zutabea       | Mota          | Deskribapena                                      |
|---------------|---------------|---------------------------------------------------|
| `id`          | SERIAL (PK)   | Partidaren identifikatzaile bakarra               |
| `player1_id`  | INTEGER (FK)  | Lehen jokalariaren ID-a → `erabiltzaileak(id)`    |
| `player2_id`  | INTEGER (FK)  | Bigarren jokalariaren ID-a → `erabiltzaileak(id)` |
| `winner_id`   | INTEGER (FK)  | Irabazlearen ID-a → `erabiltzaileak(id)`          |
| `played_at`   | TIMESTAMP     | Partida noiz jokatu zen                           |
```

**Kontsulta Adibidea**:
```sql
SELECT COUNT(*) FROM partidak WHERE played_at BETWEEN '2025-01-01' AND '2025-12-31';
```

---

#### ~~Taula: **partida_kola**~~ (EZ ERABILIA)
**Oharra**: Taula hau ez da erabiltzen. Kola zerbitzariaren memorian kudeatzen da `List<BezeroKonektatuaDatuBasean>` erabiliz, efizientzia hobetzeko.

---

### Erlazioak

- **Erabiltzaileak ↔ Partidak**:
  - Erabiltzaile batek **hainbat partida** joka ditzake (`player1_id` edo `player2_id` gisa).
  - Erabiltzaile batek **hainbat partida irabazi** ditzake (`winner_id` gisa).
  - Harreman motak: `1:N` (One-to-Many)

**Foreign Keys**:
```sql
ALTER TABLE partidak
  ADD CONSTRAINT fk_player1 FOREIGN KEY (player1_id) REFERENCES erabiltzaileak(id),
  ADD CONSTRAINT fk_player2 FOREIGN KEY (player2_id) REFERENCES erabiltzaileak(id),
  ADD CONSTRAINT fk_winner  FOREIGN KEY (winner_id)  REFERENCES erabiltzaileak(id);
```

---

## Funtzionaltasun Tekniko Garrantzitsuak

### TCP/IP Komunikazioa
**Deskribapena**: Zerbitzaria eta bezeroa TCP socket-en bidez komunikatzen dira. Zerbitzaria `TcpListener` erabiltzen du bezero-konexioak onartzeko eta `StreamWriter`/`StreamReader` mezuak bidaltzeko/jasotzeko.

**Protokoloa**: Mezuak testu-formatuan bidaltzen dira `:` zatitzailearekin.
- Adibideak: 
  - `LOGIN:erabiltzailea:pasahitza`
  - `MOVE:jokalaria:row,col:pieza`
  - `CHAT:bidaltzailea:mezua`

**Kodea**: `Server.cs` → `Main()`, `ErabiltzaileaKudeatuAsync()`

---

### Partida-kudeaketa
**Deskribapena**: Partidak `Partida.cs` klasean kudeatzen dira. Bakoitzak 15x15 taula bat du, txanda-kontrola, txata eta irabazle-detekzioa (5 jarraian).

**Egitura**:
- **Taula**: `string[15,15]` → `"B"` (beltza), `"W"` (txuria), `null` (hutsik)
- **Txanda**: `TxandakoJokalaria` atributua
- **Irabazle-detekzioa**: 4 norabidetan konprobatu (horizontala, bertikala, diagonala)

**Kodea**: `Partida.cs` → `ProzesatuMugimendua()`, `EgiaztatuIrabazlea()`

---

### Autentikazioa eta Erabiltzaile-kudeaketa
**Deskribapena**: Erabiltzaileak datu-basean autentikatzen dira. Admin-ek erabiltzaileak sor/ezaba/aldatu ditzakete.

**Funtzionalitateak**:
- Login: `databaseOperations.checkErabiltzaileak()`
- Signup: `databaseOperations.sortuErabiltzailea()`
- Admin konprobaketa: `databaseOperations.checkAdmin()`
- Pasahitza aldatu: `databaseOperations.aldatuPasahitza()`

**Kodea**: `databaseOperations.cs`

---

### ELO Sistema
**Deskribapena**: Partidak amaitzean, ELO puntuazioa automatikoki eguneratzen da:
- Irabazlea: **+100 ELO**
- Galtzailea: **-100 ELO**

**Kodea**: `databaseOperations.cs` → `partidaGorde()`

---

### PDF Txostenak
**Deskribapena**: Admin-ek PDF txostenak sor ditzakete **QuestPDF** liburutegia erabiliz:
- **TOP 10** ranking-a
- Erabiltzaile baten **estatistikak** (ELO, irabaziak, galduak, winrate)
- **Partida-kopurua** data-tarte batean

**Kodea**: `PdfExport.cs` → `Top10Document`, `UserStatsDocument`, `PartidaKopuruaDocument`

---

### Matchmaking Sistema
**Deskribapena**: Erabiltzaileak partida-kolan sartzen dira `FIND_MATCH` aginduarekin. Zerbitzariak kolatik bi jokalari hartzen ditu eta partida bat sortzen du.

**Kola**: `List<BezeroKonektatuaDatuBasean> kolanDaudenErabiltzaileak`

**Kodea**: `Server.cs` → `findMatchKudeatu()`, `startMatchKudeatu()`

---

### Thread Segurtasuna
**Deskribapena**: Zerbitzariak hainbat bezero aldi berean kudeatu behar dituenez, `lock(lockObject)` erabiltzen da datu partekatuetara sarbidea sinkronizatzeko.

**Datu partekatuak**:
- `zerbitzarikoBezeroak` (konektatutako bezeroak)
- `kolanDaudenErabiltzaileak` (matchmaking kola)
- `partidaAktiboak` (jolasten ari diren partidak)

**Kodea**: `Server.cs` → `lock(lockObject) { ... }`

---

### Txat Sistema
**Deskribapena**: Partidan dauden bi jokalariok txat-mezuak trukatutz ditzakete denbora errealean.

**Protokoloa**: `CHAT:bidaltzailea:mezua`

**Kodea**: `Partida.cs` → `ProzesatuChatMezua()`

---

## Metodoak / Funtzio Garrantzitsuak Dokumentatzea

### Metodo: `Server.Main()`
**Klasea**: `Server.cs`

**Deskribapena**: Zerbitzariaren sarrera-puntua. TCP listener-a abiarazten du, datu-basera konektatzen da eta bezero-konexioak onartzen ditu bukle infinitu batean.

**Parametroak**:
```bash
| Parametroa | Mota       | Deskribapena                     |
|------------|------------|----------------------------------|
| `args`     | `string[]` | Komando-lerroko argumentuak      |
```
**Itzulera-balioa**: `Task` (asinkronoa)

**Salbuespenak**: `Exception` (konexio-erroreak)

**Erabilera-adibidea**:
```csharp
await Server.Main(args);
```

**Fluxua**:
1. TCP listener sortu `IPAddress.Any:13000`-n
2. Datu-basera konektatu
3. Bukle infinituan bezero berriak onartu
4. Bezero bakoitzeko hari bat abiarazi (`ErabiltzaileaKudeatuAsync`)

---

### Metodo: `databaseOperations.checkErabiltzaileak()`
**Klasea**: `databaseOperations.cs`

**Deskribapena**: Erabiltzaile bat eta pasahitza datu-basean existitzen diren egiaztatzen du.

**Parametroak**:
```bash
| Parametroa     | Mota     | Deskribapena                |
|----------------|----------|-----------------------------|
| `erabiltzaile` | `string` | Erabiltzaile-izena          |
| `pasahitza`    | `string` | Pasahitza                   |
```
**Itzulera-balioa**: `Task<bool>` → `true` existitzen bada, `false` bestela

**Salbuespenak**: `Exception` (SQL erroreak)

**Erabilera-adibidea**:
```csharp
bool exists = await databaseOperations.checkErabiltzaileak("admin", "admin");
if (exists) {
    Console.WriteLine("Login arrakastatsua");
}
```

**SQL Kontsulta**:
```sql
SELECT COUNT(*) FROM erabiltzaileak 
WHERE TRIM(username) = @erabiltzaile AND TRIM(password) = @pasahitza;
```

---

### Metodo: `Partida.ProzesatuMugimendua()`
**Klasea**: `Partida.cs`

**Deskribapena**: Jokalari batek egindako mugimendua prozesatzen du: txanda egiaztatzen du, laukia hutsik dagoen konprobatzen du, mugimendua taulan gordetzen du eta irabazlea detektatzen du.

**Parametroak**:
```bash
| Parametroa  | Mota     | Deskribapena                        |
|-------------|----------|-------------------------------------|
| `jokalaria` | `string` | Mugimendu hau egin duen jokalaria   |
| `row`       | `int`    | Taularen lerroa (0-14)              |
| `col`       | `int`    | Taularen zutabea (0-14)             |
```
**Itzulera-balioa**: `bool` → `true` mugimendua baliozkoa bada, `false` bestela

**Salbuespenak**: Ez du salbuespen espliziturik jaurti

**Erabilera-adibidea**:
```csharp
bool success = partida.ProzesatuMugimendua("user1", 7, 7);
if (success) {
    Console.WriteLine("Mugimendua ondo egin da");
}
```

**Fluxua**:
1. Txanda egiaztatu (`TxandakoJokalaria == jokalaria`)
2. Laukia hutsik dagoen egiaztatu (`Taula[row, col] == null`)
3. Pieza taulan jarri (`"B"` edo `"W"`)
4. Mezua biei bidali: `MOVE:jokalaria:row,col:pieza`
5. Txanda aldatu
6. Irabazlea egiaztatu (5 jarraian)
7. Irabazlea badago, partida amaitu

---

### Metodo: `Partida.EgiaztatuIrabazlea()`
**Klasea**: `Partida.cs`

**Deskribapena**: Mugimendua egin ondoren, ea 5 pieza jarraian dauden konprobatzen du 4 norabidetan (horizontala, bertikala, 2 diagonalak).

**Parametroak**:
```bash
| Parametroa | Mota     | Deskribapena                            |
|------------|----------|-----------------------------------------|
| `row`      | `int`    | Azken mugimenduaren lerroa              |
| `col`      | `int`    | Azken mugimenduaren zutabea             |
| `pieza`    | `string` | Jarritako pieza (`"B"` edo `"W"`)       |
```
**Itzulera-balioa**: `bool` → `true` 5 jarraian badaude, `false` bestela

**Salbuespenak**: Ez du salbuespen espliziturik jaurti

**Erabilera-adibidea**:
```csharp
bool irabazi = partida.EgiaztatuIrabazlea(7, 7, "B");
if (irabazi) {
    Console.WriteLine("Jokalari1 irabazi du!");
}
```

**Algoritmo Deskribapena**:
```
4 NORABIDE KONPROBATU:
1. [0,1]   → Horizontala (→ ←)
2. [1,0]   → Bertikala (↓ ↑)
3. [1,1]   → Diagonala (↘ ↖)
4. [1,-1]  → Diagonala (↙ ↗)

Norabide bakoitzean:
  - Hasierako pieza zenbatu (1)
  - Norabide positiboan zenbatu (gehienez 4)
  - Norabide negatiboan zenbatu (gehienez 4)
  - TOTALA >= 5 bada → IRABAZLEA!
```

**Adibidea**:
```
Taula (zatia):
     3   4   5   6   7
  2  B   .   .   .   .
  3  .   B   .   .   .
  4  .   .   B*  .   .  ← Orain jarri du [4,5]
  5  .   .   .   B   .
  6  .   .   .   .   B

Diagonala [1,1] konprobaketa:
  - Hasiera: 1 (Taula[4,5])
  - Positiboa (+1,+1): Taula[5,6]="B" ✓, Taula[6,7]="B" ✓ → 2
  - Negatiboa (-1,-1): Taula[3,4]="B" ✓, Taula[2,3]="B" ✓ → 2
  - TOTALA: 1 + 2 + 2 = 5 → IRABAZLEA!
```

---

### Metodo: `databaseOperations.partidaGorde()`
**Klasea**: `databaseOperations.cs`

**Deskribapena**: Partida bat datu-basean gordetzen du eta bi jokalarien ELO-a eguneratzen du (+100 irabazlea, -100 galtzailea).

**Parametroak**:
```bash
| Parametroa | Mota     | Deskribapena                    |
|------------|----------|---------------------------------|
| `player1`  | `string` | Lehen jokalariaren izena        |
| `player2`  | `string` | Bigarren jokalariaren izena     |
| `winner`   | `string` | Irabazlearen izena              |
| `loser`    | `string` | Galtzailearen izena             |
```
**Itzulera-balioa**: `void`

**Salbuespenak**: `Exception` (SQL erroreak)

**Erabilera-adibidea**:
```csharp
databaseOperations.partidaGorde("user1", "user2", "user1", "user2");
```

**SQL Kontsultak**:
```sql
-- 1. Partida sartu
INSERT INTO partidak (player1_id, player2_id, winner_id, played_at)
VALUES (
    (SELECT id FROM erabiltzaileak WHERE TRIM(username) = @player1),
    (SELECT id FROM erabiltzaileak WHERE TRIM(username) = @player2),
    (SELECT id FROM erabiltzaileak WHERE TRIM(username) = @winner),
    NOW()
);

-- 2. ELO eguneratu
UPDATE erabiltzaileak SET elo = elo+100 WHERE TRIM(username) = @winner;
UPDATE erabiltzaileak SET elo = elo-100 WHERE TRIM(username) = @loser;
```

---

### Metodo: `Top10Document.Compose()`
**Klasea**: `PdfExport.cs`

**Deskribapena**: TOP 10 ranking-a PDF dokumentu gisa sortzen du QuestPDF liburutegia erabiliz.

**Parametroak**:
```
| Parametroa  | Mota                | Deskribapena                |
|-------------|---------------------|-----------------------------|
| `container` | `IDocumentContainer`| PDF dokumentuaren edukiontzia|
```
**Itzulera-balioa**: `void`

**Salbuespenak**: Ez du salbuespen espliziturik jaurti

**Erabilera-adibidea**:
```csharp
var doc = new Top10Document(topJokalariak);
string path = Path.Combine(PdfExport.EnsureOutputFolder(), "top10.pdf");
doc.GeneratePdf(path);
PdfExport.OpenFile(path);
```

**Egitura**:
```
┌────────────────────────────────────┐
│ Txuribeltz - TOP 10                │
│ Leaderboard export                 │
├────┬───────────────────┬───────────┤
│ #  │ Player            │ ELO       │
├────┼───────────────────┼───────────┤
│ 1  │ admin             │ 1500      │
│ 2  │ user1             │ 1200      │
│ ...│ ...               │ ...       │
└────┴───────────────────┴───────────┘
Generated: 2025-06-15 14:30  |  Page 1/1
```

---

### Metodo: `ValidationService.ValidateLogin()`
**Klasea**: `ValidationService.cs`

**Deskribapena**: Login formularioko datuak balidatzen ditu (erabiltzailea eta pasahitza hutsik ez daudela egiaztatzen du).
```
**Parametroak**:
| Parametroa | Mota     | Deskribapena              |
|------------|----------|---------------------------|
| `username` | `string` | Erabiltzaile-izena        |
| `password` | `string` | Pasahitza                 |
```
**Itzulera-balioa**: `ValidationResult` → `IsValid` eta `ErrorMessage` dituen record-a

**Erabilera-adibidea**:
```csharp
var validation = validationService.ValidateLogin(txtUsuario.Text, txtPassword.Password);
if (!validation.IsValid) {
    txt_erroreak.Text = validation.ErrorMessage;
    return;
}
// Login zerbitzarira bidali...
```

**Balioztatze-arauak**:
- Erabiltzailea eta pasahitza **ez dute hutsik egon behar**
- Ez da formatua egiaztatzen (adibidez, luzera, karaktere bereziak)

---

## Errore Ezagunak eta Mugak

### Segurtasun Arazoak

#### **Pasahitzak enkriptatu gabe**
**Deskribapena**: Pasahitzak testu arruntean gordetzen dira datu-basean. Ez dago hash algoritmoen (bcrypt, Argon2) erabilpenik.

**Arriskua**: Datu-basea sarbide ez-baimendunak jasaten badu, pasahitz guztiak eskuragarri egongo dira.

**Konponbidea**: Inplementatu pasahitz-hash-a `BCrypt.Net` edo `ASP.NET Core Identity` erabiliz.

---

#### **SQL Injection prebentzioa**
**Deskribapena**: Npgsql parametrizatuak erabiltzen dira (`@parametroa`), baina input balidazio gehigarririk ez dago.

**Egoera**: Babestuta (parametrizazio bidez)

---

#### **TCP komunikazio enkriptatu gabe**
**Deskribapena**: Mezuak testu arruntean bidaltzen dira TCP socket-en bidez. Ez dago SSL/TLS enkriptaziorik.

**Arriskua**: Man-in-the-middle erasoak mezuak irakurri edo aldatu ditzake.

**Konponbidea**: Inplementatu `SslStream` TCP socket-en gainean.

---

### Eskalagarritasun Mugak

#### **Bezero maximoa: 10**
**Deskribapena**: Zerbitzariak gehienez 10 bezero aldi berean onartzen ditu.

**Kodea**:
```csharp
private const int MaxBezeroak = 10;
```

**Arrazoia**: Thread kudeaketa sinplea mantentzeko eta baliabideak mugatzeko.

---

### Erabilgarritasun Arazoak

#### **Matchmaking ez da ELO-basatua**
**Deskribapena**: Partida bat bilatzen dutenean, lehenengo bi jokalariok parekatzen dira, ELO kontuan hartu gabe.

**Eragina**: ELO oso desberdina duten jokalariak elkarren aurka jolas dezakete.

**Konponbidea**: Inplementatu ELO-basatutako matchmaking algoritmoa:


### Kode-kalitate Arazoak

#### **Unit test gutxi**
**Deskribapena**: Soilik `ValidationService` klaseak dauka unit testak. Gainerako kodeak ez du testaketarik.

**Eragina**: Erregresioak detektatzea zaila da.

**Konponbidea**: Gehitu testak `databaseOperations`, `Partida` eta `Server` klaseetarako.

---

#### **Erroreen kudeaketa ez da homogeneoa**
**Deskribapena**: Metodo batzuek `Exception` jaurti, beste batzuek `Console.WriteLine` erabiltzen dute eta beste batzuek `bool` itzultzen dute.

**Konponbidea**: Definitu errore-kudeaketa politika homogeneoa (logger-a erabili, custom salbuespenak, etab.).

---

#### **Logger-ik ez**
**Deskribapena**: `Console.WriteLine` erabiltzen da log guztietarako. Ez dago log-mailak (INFO, WARN, ERROR) edo fitxategi-logging-ik.

**Konponbidea**: Inplementatu logger-a (`Serilog`, `NLog` edo `Microsoft.Extensions.Logging`).

### Zeharkako Arazoak

#### **Docker dependentzia**
**Deskribapena**: Aplikazioak Docker behar du datu-basea exekutatzeko. Docker instalatu gabe, aplikazioa ez da funtzionatzen.

### Etorkizuneko Hobekuntzak

- **SSL/TLS enkriptazioa** TCP komunikazioan
- **Pasahitz-hash-a** (bcrypt, Argon2)
- **Logger-a** (Serilog, NLog)
- **Unit testak** gehiago
- **ELO-basatutako matchmaking-a**

---

## Amaiera

Dokumentu honek Txuribeltz proiektuaren funtzionaltasun teknikoak, metodoak eta ezagutzen diren mugak deskribatzen ditu. Hobekuntza posibleak etorkizuneko bertsioetarako proposatzen dira.

**Bertsio**: 1.0  
**Data**: 2025-06-15  
**Egilea**: Aitor Gaillard