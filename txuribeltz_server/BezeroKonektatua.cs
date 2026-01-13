using System.Net.Sockets;
using System.IO;

public class BezeroKonektatua
{
    public TcpClient SocketCliente { get; set; }
    public string Erabiltzailea { get; set; }
    public string Mota { get; set; }
    public StreamWriter Writer { get; set; }
    public StreamReader Reader { get; set; }
}