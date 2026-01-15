using System.Net;
using System.Net.Sockets;
using System.Text;
using txuribeltz_server;


// Zerbitzariaren klase nagusia. TCP konexioak kudeatzen ditu eta bezeroei zerbitzuak eskaintzen dizkie.

public class Server
{
    private static readonly object lockObject = new();
    // Konektatutako bezeroen zerrenda (zerbitzarian baina logeatu gabe)
    private static readonly List<BezeroKonektatua> bezeroak = [];
    // gehienezko bezero kopurua
    private const int MaxBezeroak = 10;

    public static async Task Main(string[] args)
    {
        // Zerbitzariaren datuak
        string servidor = "127.0.0.1";
        IPAddress ipserver = IPAddress.Parse(servidor);
        int port = 13000;

        // TCP listener sortu eta martxan jarri
        TcpListener listener = new(ipserver, port);
        Console.WriteLine("Zerbitzaria martxan dago {0}:{1}", servidor, port);
        listener.Start();

        // Database konexioa sortu, lortzen duenean aurrera jarraituko du
        await databaseOperations.ConnectDatabaseAsync();

        try
        {
            // Bezero berriak onartzen jarraitu
            while (true)
            {
                TcpClient socketCliente = await listener.AcceptTcpClientAsync();
                lock (lockObject)
                {
                    if (bezeroak.Count >= MaxBezeroak)
                    {
                        Console.WriteLine("Bezero gehiegi konektatuta. Ezin da konektatu.");
                        socketCliente.Close();
                        continue;
                    }
                    Console.WriteLine("Bezero bat konektatuta");
                }
                // Bezeroaren haria abiarazi
                _ = Task.Run(() => ErabiltzaileaKudeatuAsync(socketCliente));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errorea zerbitzarian: " + ex.Message);
        }
    }

    private static async Task ErabiltzaileaKudeatuAsync(TcpClient socketCliente)
    {
        StreamReader? reader = null;
        StreamWriter? writer = null;
        BezeroKonektatua? logeatutakoBezeroa = null;

        try
        {
            NetworkStream ns = socketCliente.GetStream();
            writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
            reader = new StreamReader(ns, Encoding.UTF8);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                string[] mezuarenzatiak = line.Split(':');
                string agindua = mezuarenzatiak[0];

                logeatutakoBezeroa = await ProzesatuAgindua(agindua, mezuarenzatiak, writer, reader, socketCliente, logeatutakoBezeroa);

                if (agindua == "DISCONNECT")
                {
                    Console.WriteLine("Bezeroak deskonexioa eskatu du");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errorea bezeroaren hariarekin: {ex.Message}");
        }
        finally
        {
            KenduBezeroa(logeatutakoBezeroa);
            reader?.Close();
            writer?.Close();
            socketCliente?.Close();
            Console.WriteLine("Bezeroa deskonektatuta");
        }
    }

    private static async Task<BezeroKonektatua?> ProzesatuAgindua(
        string agindua,
        string[] mezuarenzatiak,
        StreamWriter writer,
        StreamReader reader,
        TcpClient socketCliente,
        BezeroKonektatua? logeatutakoBezeroa)
    {
        switch (agindua)
        {
            case "LOGIN" when mezuarenzatiak.Length == 3:
                return await LoginKudeatu(mezuarenzatiak, writer, reader, socketCliente);

            case "SIGNUP" when mezuarenzatiak.Length == 3:
                await SignupKudeatu(mezuarenzatiak, writer);
                break;

            case "GET_USERS" when logeatutakoBezeroa?.Mota == "admin":
                GetUsersKudeatu(logeatutakoBezeroa, writer);
                break;

            case "DELETE" when mezuarenzatiak.Length > 1:
                databaseOperations.ezabatuErabiltzailea(mezuarenzatiak[1]);
                break;

            case "CHANGE_P" when mezuarenzatiak.Length >= 3:
                databaseOperations.aldatuPasahitza(mezuarenzatiak[1], mezuarenzatiak[2]);
                break;

            case "DISCONNECT":
                // metodoan ez dago ezer egin behar, haria itxi egingo da main metodoko finally blokean
                break;

            default:
                writer.WriteLine("ERROR:Agindu ezezaguna edo parametro falta");
                break;
        }

        return logeatutakoBezeroa;
    }

    private static async Task<BezeroKonektatua?> LoginKudeatu(

        string[] mezuarenzatiak,
        StreamWriter writer,
        StreamReader reader,
        TcpClient socketCliente)
    {
        string erabiltzailea = mezuarenzatiak[1];
        string pasahitza = mezuarenzatiak[2];

        bool exists = await databaseOperations.checkErabiltzaileak(erabiltzailea, pasahitza);
        if (!exists)
        {
            writer.WriteLine("LOGIN_FAIL:Erabiltzailea edo pasahitza okerra, badaukazu erabiltzailerik?");
            return null;
        }

        if (ErabiltzaileaLogeatuta(erabiltzailea))
        {
            writer.WriteLine("LOGIN_FAIL:Erabiltzailea jadanik logeatuta dago");
            return null;
        }

        bool isAdmin = await databaseOperations.checkAdmin(erabiltzailea);
        string mota = isAdmin ? "admin" : "user";

        writer.WriteLine($"LOGIN_OK:{mota}");
        Console.WriteLine($"{mota.ToUpper()} erabiltzailea logeatuta: {erabiltzailea}");

        var bezeroBerria = new BezeroKonektatua
        {
            Erabiltzailea = erabiltzailea,
            SocketCliente = socketCliente,
            Mota = mota,
            Writer = writer,
            Reader = reader
        };

        GehituBezeroa(bezeroBerria);
        return bezeroBerria;
    }

    private static Task SignupKudeatu(string[] mezuarenzatiak, StreamWriter writer)
    {
        string erabiltzailea = mezuarenzatiak[1];
        string pasahitza = mezuarenzatiak[2];

        databaseOperations.sortuErabiltzailea(erabiltzailea, pasahitza);
        Console.WriteLine($"SIGNUP_OK:{erabiltzailea}");
        writer.WriteLine("SIGNUP_OK");

        return Task.CompletedTask;
    }

    private static void GetUsersKudeatu(BezeroKonektatua logeatutakoBezeroa, StreamWriter writer)
    {
        Console.WriteLine($"Admin: {logeatutakoBezeroa.Erabiltzailea} erabiltzaile zerrenda eskatzen");
        List<Erabiltzaile> erabiltzaileak = databaseOperations.kargatuErabiltzaileak();

        string erabiltzaileZerrenda = string.Join(";",
            erabiltzaileak.Select(u => $"{u.Erabiltzailea}|{u.Mota}|{u.Pasahitza}"));

        writer.WriteLine($"USERS_LIST:{erabiltzaileZerrenda}");
        Console.WriteLine($"Erabiltzaile zerrenda bidalita: {erabiltzaileak.Count} erabiltzaile");
    }

    private static bool ErabiltzaileaLogeatuta(string erabiltzailea)
    {
        lock (lockObject)
        {
            return bezeroak.Any(b => b.Erabiltzailea == erabiltzailea);
        }
    }

    private static void GehituBezeroa(BezeroKonektatua bezeroa)
    {
        lock (lockObject)
        {
            bezeroak.Add(bezeroa);
            Console.WriteLine($"LOGEATUTAKO BEZERO KOPURUA: {bezeroak.Count}");
        }
    }

    private static void KenduBezeroa(BezeroKonektatua? bezeroa)
    {
        if (bezeroa == null) return;

        lock (lockObject)
        {
            bezeroak.Remove(bezeroa);
            Console.WriteLine($"Bezeroa {bezeroa.Erabiltzailea} zerrendatik kenduta");
            Console.WriteLine($"LOGEATUTAKO BEZERO KOPURUA: {bezeroak.Count}");
        }
    }
}
