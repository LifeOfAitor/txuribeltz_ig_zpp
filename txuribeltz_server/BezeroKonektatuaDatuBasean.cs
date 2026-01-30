using System.Net.Sockets;

// Datu basean konektatutako bezeroaren informazioa gordetzeko klasea
// Socket, erabiltzaile izena, mota, elo, StreamWriter eta StreamReader eta partidan badago, partidaren ID gordeko da.
// Ez dira zertan gorde behar datu guztiak, beharraren arabera gordetzen joango dira
public class BezeroKonektatuaDatuBasean
{
    public TcpClient SocketCliente { get; set; }
    public string Erabiltzailea { get; set; }
    public string Mota { get; set; }
    public string Elo { get; set; }
    public StreamWriter Writer { get; set; }
    public StreamReader Reader { get; set; }
    public string PartidaID { get; set; }

    // Debugeatzeko erabili daiteke
    public override string ToString()
    {
        return $"Erabiltzailea: {Erabiltzailea}\n";
    }
}