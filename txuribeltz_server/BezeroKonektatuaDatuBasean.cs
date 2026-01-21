using System.Net.Sockets;
using System.IO;

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