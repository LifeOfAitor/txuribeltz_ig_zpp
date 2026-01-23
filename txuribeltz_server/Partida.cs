using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using txuribeltz_server;
public class Partida
{
    public string PartidaID { get; set; }
    public BezeroKonektatuaDatuBasean Jokalari1 { get; set; }
    public BezeroKonektatuaDatuBasean Jokalari2 { get; set; }
    public string Irabazlea { get; set; }
    public string Galtzailea { get; set; }
    public bool Amaituta { get; set; }
    public List<string> TxatMezuak { get; set; }

    //Jokuaren egoera gordetzeko atributuak
    public string[,] Taula { get; set; } //15x15 taula nik ezarrita
    // NOLA GORDETZEN DA INFORMAZIOA TAULAN:
    // Hutsik dagoen laukia: string.Empty edo null
    // Jokalari1-ek jarritako pieza: "B" (Black)
    // Jokalari2-ek jarritako pieza: "W" (White)
    // ORDUAN: TAULA[lerroa, zutabea] = "B" edo "W" edo string.Empty/null
    public string TxandakoJokalaria { get; set; } //Nori tokatzen zaio jolastea (txandak)

    public Partida(string partidaID, BezeroKonektatuaDatuBasean jokalari1, BezeroKonektatuaDatuBasean jokalari2)
    {
        PartidaID = partidaID;
        Jokalari1 = jokalari1;
        Jokalari2 = jokalari2;
        Irabazlea = string.Empty;
        Galtzailea = string.Empty;
        Amaituta = false;
        TxatMezuak = new List<string>();
        Taula = new string[15, 15]; //15x15 taula hasieratzea
        TxandakoJokalaria = Jokalari1.Erabiltzailea; //Jokalari1-ek hasiko du bera izan delako partida bilatu duen lehenendoa

        //Console.WriteLine($"DEBUG: Partida objetua sortu da errorerik gabe: {PartidaID} ({jokalari1.Erabiltzailea} vs {jokalari2.Erabiltzailea})");
    }

    // partidaren barruan dauden bi erabiltzaileei mezua bidaltzeko metodoa
    // edozein mezu izan daiteke, txat, mugimendua edo beste zeozer
    public void BidaliBieiei(string mezua)
    {
        try
        {
            Jokalari1.Writer.WriteLine(mezua);
            Jokalari2.Writer.WriteLine(mezua);
            //Console.WriteLine($"DEBUG: [{PartidaID}] Bidalita bieiei: {mezua}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: [{PartidaID}] Mezua bidaltzean: {ex.Message}");
        }
    }

    // partidaren barruan dagoen erabiltzaile konkretu bati mezua bidaltzeko metodoa
    // edozein mezu izan daiteke, txat, mugimendua edo beste zeozer
    public void BidaliJokalariari(string erabiltzailea, string mezua)
    {
        try
        {
            // konprobatu zein den erabiltzailea eta horren arabera mezua bidali
            if (Jokalari1.Erabiltzailea == erabiltzailea)
            {
                Jokalari1.Writer.WriteLine(mezua);
            }
            else if (Jokalari2.Erabiltzailea == erabiltzailea)
            {
                Jokalari2.Writer.WriteLine(mezua);
            }
            //Console.WriteLine($"DEBUG: [{PartidaID}] Bidalita {erabiltzailea}: {mezua}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: [{PartidaID}] Mezua {erabiltzailea} bidaltzean: {ex.Message}");
        }
    }

    // txateko mezuen kudeaketa, bidaltzailea eta mezua jasotzen ditu eta TxatMezuak listan gordetzen ditu
    // azkenik biei bidaltzen die mezua
    public void ProzesatuChatMezua(string bidaltzailea, string mezua)
    {
        string chatMezua = $"{bidaltzailea.ToUpper()}: {mezua}";
        TxatMezuak.Add(chatMezua);

        // Bidali biei
        BidaliBieiei($"CHAT:{bidaltzailea}:{mezua}");
        //Console.WriteLine($"DEBUG: [{PartidaID}] Chat: {chatMezua}");
    }

    // partidaren barruan jokalariarekiko mugimendua prozesatzen duen metodoa
    public bool ProzesatuMugimendua(string jokalaria, int row, int col)
    {
        // Egiaztatu txanda
        if (TxandakoJokalaria != jokalaria)
        {
            BidaliJokalariari(jokalaria, "ERROR:Ez da zure txanda");
            //Console.WriteLine($"DEBUG: [{PartidaID}] {jokalaria} txandaz kanpo jokatu nahi izan du");
            return false;
        }

        // Egiaztatu laukia hutsik dagoen
        if (!string.IsNullOrEmpty(Taula[row, col]))
        {
            BidaliJokalariari(jokalaria, "ERROR:Laukia okupatuta dago");
            //Console.WriteLine($"DEBUG: [{PartidaID}] {jokalaria} laukia okupatuta: [{row},{col}]");
            return false;
        }

        // Mugimendua egin
        string pieza;
        if (jokalaria == Jokalari1.Erabiltzailea)
        {
            pieza = "B"; // Jokalari1 = Beltzak (Black)
        }
        else
        {
            pieza = "W"; // Jokalari2 = Txuriak (White)
        }
        Taula[row, col] = pieza;

        string mugimendua = $"{jokalaria}:{row},{col}";

        // Bidali biei
        BidaliBieiei($"MOVE:{jokalaria}:{row},{col}:{pieza}");
        //Console.WriteLine($"DEBUG: [{PartidaID}] Mugimendua: {mugimendua}");

        // Aldatu txanda
        TxandakoJokalaria = LortuAurkalaria(jokalaria);
        BidaliBieiei($"TURN:{TxandakoJokalaria}");

        // Egiaztatu irabazlea
        if (EgiaztatuIrabazlea(row, col, pieza))
        {
            AmaituPartida(jokalaria);
        }

        return true;
    }

    // mugimendu bat egiten den momentu bakoitzean egiten da konprobazioa ea badagoen irabazle bat
    // konprobatuko da ea jarri den fitxaren inguruan badauden 5 jarraian
    private bool EgiaztatuIrabazlea(int row, int col, string pieza)
    {
        // Konprobatuko da jarritako fitxaren inguruan ea dauden 5 jarraian
        // Kontuan izan behar da atzera 4 eta aurrera beste 4 begiratu beharko dira
        // Kontuan izan behar dugu erebai bertikalki, horizontalki eta diagonalki begiratuko dugula
        /*
         * ============================================================
         * ADIBIDEA: Jokalari1 (B) fitxa bat jarri du [4,5] posizioan
         * ============================================================
         * 
         * TAULA EGOERA (15x15 taulatik zati bat):
         * 
         *        Col:  0    1    2    3    4    5    6    7    8
         *            ┌────┬────┬────┬────┬────┬────┬────┬────┬────┐
         *    Row 0   │    │    │    │    │    │    │    │    │    │
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 1   │    │    │    │    │    │    │    │    │    │
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 2   │    │    │    │ B  │    │    │    │    │    │  ← Taula[2,3] = "B"
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 3   │    │    │    │    │ B  │    │    │    │    │  ← Taula[3,4] = "B"
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 4   │    │    │    │    │    │ B* │    │    │    │  ← Taula[4,5] = "B" ← ORAIN JARRI DU!
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 5   │    │    │ W  │    │    │    │ B  │    │    │  ← Taula[5,6] = "B"
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 6   │    │    │ W  │    │    │    │    │ B  │    │  ← Taula[6,7] = "B" 
         *            ├────┼────┼────┼────┼────┼────┼────┼────┼────┤
         *    Row 7   │    │    │ W  │    │    │    │    │    │    │
         *            └────┴────┴────┴────┴────┴────┴────┴────┴────┘
         * 
         * ============================================================
         * 4 NORABIDE KONPROBATU BEHAR DIRA:
         * ============================================================
         * 
         * 1. HORIZONTALA [0,1]: → eskuinera eta ← ezkerrera
         *    Taula[4,5] → Taula[4,6], Taula[4,7]... (hutsik) = 0
         *    Taula[4,5] → Taula[4,4], Taula[4,3]... (hutsik) = 0
         *    TOTALA: 1 (hasierakoa) + 0 + 0 = 1 ❌ Ez da 5
         * 
         * 2. BERTIKALA [1,0]: ↓ behera eta ↑ gora
         *    Taula[4,5] → Taula[5,5], Taula[6,5]... (hutsik) = 0
         *    Taula[4,5] → Taula[3,5], Taula[2,5]... (hutsik) = 0
         *    TOTALA: 1 + 0 + 0 = 1 ❌ Ez da 5
         * 
         * 3. DIAGONALA [1,1]: ↘ behera-eskuinera eta ↖ gora-ezkerrera
         *    Taula[4,5] → Taula[5,6]="B" ✓, Taula[6,7]="B" ✓ = 2
         *    Taula[4,5] → Taula[3,4]="B" ✓, Taula[2,3]="B" ✓ = 2
         *    TOTALA: 1 + 2 + 2 = 5 ✅ IRABAZLEA!
         * 
         * 4. DIAGONALA [1,-1]: ↙ behera-ezkerrera eta ↗ gora-eskuinera
         *    (Hau ez da konprobatuko, aurreko norabidean irabazlea aurkitu delako)
         * 
         * ============================================================
         * NORABIDE BAKOITZEAN NOLA FUNTZIONATZEN DUEN:
         * ============================================================
         * 
         * Adibidez, DIAGONALA [1,1] norabidean:
         * 
         * Norabide POSITIBOAN (+1,+1): ↘
         *   i=1: newRow=4+1=5, newCol=5+1=6 → Taula[5,6]="B" ✓ kopurua++
         *   i=2: newRow=4+2=6, newCol=5+2=7 → Taula[6,7]="B" ✓ kopurua++
         *   i=3: newRow=4+3=7, newCol=5+3=8 → Taula[7,8]=null ✗ GELDITU
         *   Emaitza: 2
         * 
         * Norabide NEGATIBOAN (-1,-1): ↖
         *   i=1: newRow=4-1=3, newCol=5-1=4 → Taula[3,4]="B" ✓ kopurua++
         *   i=2: newRow=4-2=2, newCol=5-2=3 → Taula[2,3]="B" ✓ kopurua++
         *   i=3: newRow=4-3=1, newCol=5-3=2 → Taula[1,2]=null ✗ GELDITU
         *   Emaitza: 2
         * 
         * TOTALA: 1 (hasierakoa) + 2 (positiboa) + 2 (negatiboa) = 5 → IRABAZLEA!
         */

        // Norabideak: {row aldaketa, col aldaketa}
        int[][] norabideak = new int[][]
        {
            new int[] {0, 1},  // Horizontala
            new int[] {1, 0},  // Bertikala
            new int[] {1, 1},  // Diagonala goitik behera
            new int[] {1, -1}  // Diagonala behetik gora
        };
        foreach (var norabidea in norabideak)
        {
            int kopurua = 1; // Hasierako pieza zenbatu

            // Norabide positiboan zenbatu
            kopurua += ZenbatuNorabidean(row, col, norabidea[0], norabidea[1], pieza);

            // Norabide negatiboan zenbatu (kontrakoa)
            kopurua += ZenbatuNorabidean(row, col, -norabidea[0], -norabidea[1], pieza);

            if (kopurua >= 5)
            {
                //Console.WriteLine($"DEBUG: 5 jarraian aurkituta! Norabidea: [{norabidea[0]},{norabidea[1]}]");
                return true;
            }
        }

        return false;
    }
    // Zenbat pieza dauden jarraian emandako norabidean hasierako posiziotik abiatuta
    private int ZenbatuNorabidean(int row, int col, int rowDir, int colDir, string pieza)
    {
        /*
         * Metodo honek zenbatzen du zenbat pieza berdin dauden jarraian
         * emandako norabidean, hasierako posiziotik abiatuta.
         * 
         * Adibidea: ZenbatuNorabidean(4, 5, 1, 1, "B")
         * Hasierako posizioa: [4,5]
         * Norabidea: [+1,+1] (diagonala behera-eskuinera)
         * 
         * i=1: [4+1, 5+1] = [5,6] → Taula[5,6]="B" ✓ kopurua=1
         * i=2: [4+2, 5+2] = [6,7] → Taula[6,7]="B" ✓ kopurua=2
         * i=3: [4+3, 5+3] = [7,8] → Taula[7,8]=null ✗ GELDITU
         * 
         * Itzultzen du: 2
         */
        int kopurua = 0;

        for (int i = 1; i < 5; i++) // Gehienez 4 laukia gehiago egiaztatu
        {
            int newRow = row + (i * rowDir);
            int newCol = col + (i * colDir);

            // Taularen mugetatik kanpo badago, gelditu
            if (newRow < 0 || newRow >= 15 || newCol < 0 || newCol >= 15)
                break;

            // Pieza berdina bada, zenbatu
            if (Taula[newRow, newCol] == pieza)
                kopurua++;
            else
                break; // Pieza ezberdina edo hutsik, gelditu
        }

        return kopurua;
    }

    // Partida jolasten ari den erabiltzailearen aurkaria lortzeko metodoa
    public string? LortuAurkalaria(string erabiltzailea)
    {
        if (Jokalari1.Erabiltzailea == erabiltzailea)
            return Jokalari2.Erabiltzailea;
        else if (Jokalari2.Erabiltzailea == erabiltzailea)
            return Jokalari1.Erabiltzailea;
        return null;
    }

    // Partida amaitzeko metodoa, irabazlea eta galtzailea jasotzen ditu eta bakoitzari bidaltzen dio dagokion mezua, bertatik kudeatzeko
    public void AmaituPartida(string? irabazlea)
    {
        Amaituta = true;
        Irabazlea = irabazlea;
        Galtzailea = LortuAurkalaria(irabazlea);
        Console.WriteLine($"[{PartidaID}] Partida amaitu - Irabazlea: {irabazlea ?? "Berdinketa"}");

        // Bidali emaitza BIEIEI
        BidaliJokalariari(Irabazlea, "MATCH_END:WIN:Irabazi duzu!");
        BidaliJokalariari(Galtzailea, "MATCH_END:LOSE:Galdu duzu!");

        // emaitza datubasean gorde
        databaseOperations.partidaGorde(Jokalari1.Erabiltzailea, Jokalari2.Erabiltzailea, Irabazlea, Galtzailea);
    }
}
