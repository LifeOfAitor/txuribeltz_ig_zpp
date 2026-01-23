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

    private bool EgiaztatuIrabazlea(int row, int col, string pieza)
    {
        // 5 jarraian logika inplementatu behar dut oraindik
        return false;
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
