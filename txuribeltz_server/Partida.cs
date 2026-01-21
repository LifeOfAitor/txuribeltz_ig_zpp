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
        TxandakoJokalaria = Jokalari1.Erabiltzailea; //Jokalari1-ek hasiko du

        Console.WriteLine($"DEBUG: Partida objetua sortu da errorerik gabe: {PartidaID} ({jokalari1.Erabiltzailea} vs {jokalari2.Erabiltzailea})");
    }

    public void BidaliBieiei(string mezua)
    {
        try
        {
            Jokalari1.Writer.WriteLine(mezua);
            Jokalari2.Writer.WriteLine(mezua);
            Console.WriteLine($"DEBUG: [{PartidaID}] Bidalita bieiei: {mezua}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: [{PartidaID}] Mezua bidaltzean: {ex.Message}");
        }
    }

    public void BidaliJokalariari(string erabiltzailea, string mezua)
    {
        try
        {
            if (Jokalari1.Erabiltzailea == erabiltzailea)
            {
                Jokalari1.Writer.WriteLine(mezua);
            }
            else if (Jokalari2.Erabiltzailea == erabiltzailea)
            {
                Jokalari2.Writer.WriteLine(mezua);
            }
            Console.WriteLine($"DEBUG: [{PartidaID}] Bidalita {erabiltzailea}ri: {mezua}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: [{PartidaID}] Mezua {erabiltzailea}ri bidaltzean: {ex.Message}");
        }
    }

    // CHAT MEZUAK KUDEATU (partida barruan)
    public void ProzesatuChatMezua(string bidaltzailea, string mezua)
    {
        string chatMezua = $"{bidaltzailea.ToUpper()}: {mezua}";
        TxatMezuak.Add(chatMezua);

        // Bidali biei
        BidaliBieiei($"CHAT:{bidaltzailea}:{mezua}");
        Console.WriteLine($"DEBUG: [{PartidaID}] Chat: {chatMezua}");
    }

    // MUGIMENDUAK KUDEATU (partida barruan!)
    public bool ProzesatuMugimendua(string jokalaria, int row, int col)
    {
        // Egiaztatu txanda
        if (TxandakoJokalaria != jokalaria)
        {
            BidaliJokalariari(jokalaria, "ERROR:Ez da zure txanda");
            Console.WriteLine($"DEBUG: [{PartidaID}] {jokalaria} txandaz kanpo jokatu nahi izan du");
            return false;
        }

        // Egiaztatu laukia hutsik dagoen
        if (!string.IsNullOrEmpty(Taula[row, col]))
        {
            BidaliJokalariari(jokalaria, "ERROR:Laukia okupatuta dago");
            Console.WriteLine($"DEBUG: [{PartidaID}] {jokalaria} laukia okupatuta: [{row},{col}]");
            return false;
        }

        // Mugimendua egin
        string pieza = (jokalaria == Jokalari1.Erabiltzailea) ? "B" : "W"; // Black edo White
        Taula[row, col] = pieza;

        string mugimendua = $"{jokalaria}:{row},{col}";

        // Bidali biei
        BidaliBieiei($"MOVE:{jokalaria}:{row},{col}:{pieza}");
        Console.WriteLine($"DEBUG: [{PartidaID}] Mugimendua: {mugimendua}");

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
        // 5 jarraian logika (horizontal, vertical, diagonal)
        return false;
    }

    public string? LortuAurkalaria(string erabiltzailea)
    {
        if (Jokalari1.Erabiltzailea == erabiltzailea)
            return Jokalari2.Erabiltzailea;
        else if (Jokalari2.Erabiltzailea == erabiltzailea)
            return Jokalari1.Erabiltzailea;
        return null;
    }

    public void AmaituPartida(string? irabazlea)
    {
        Amaituta = true;
        Irabazlea = irabazlea;
        Galtzailea = LortuAurkalaria(irabazlea);
        Console.WriteLine($"[{PartidaID}] Partida amaitu - Irabazlea: {irabazlea ?? "Berdinketa"}");

        // Bidali emaitza bieiei
        BidaliBieiei($"MATCH_END:{irabazlea}");

        // emaitza datubasean gorde
        databaseOperations.partidaGorde(Jokalari1.Erabiltzailea, Jokalari2.Erabiltzailea, Irabazlea, Galtzailea);
    }
}
