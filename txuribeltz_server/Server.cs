using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Net.Sockets;
using System.Text;
using txuribeltz_server;

public class Server {
    private static object lockObject = new object();
    private static List<BezeroKonektatua> bezeroak = new List<BezeroKonektatua>();

    public static async Task Main(string[] args)
    {
        string servidor = "127.0.0.1";
        IPAddress ipserver = IPAddress.Parse(servidor);
        int port = 13000;

        TcpListener listener = new TcpListener(ipserver, port);
        Console.WriteLine("Zerbitzaria martxan dago {0}:{1}", servidor, port);
        listener.Start();

        //datu basera konektatu
        await databaseOperations.ConnectDatabaseAsync();
        

        // erabiltzailea (oraindik logeatu gabe) zerbitzarira konektatu
        try
        {
            while (true)
            {
                TcpClient socketCliente = listener.AcceptTcpClient();
                lock (lockObject)
                {
                    if (bezeroak.Count >= 10)
                    {
                        Console.WriteLine("Bezero gehiegi konektatuta. Ezin da konektatu.");
                        socketCliente.Close();
                        continue;
                    }
                    Console.WriteLine("Bezero bat konektatuta");
                    Thread t = new Thread(() => ErabiltzaileaKudeatu(socketCliente));
                    t.Start();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Errorea zerbitzarian: " + ex.Message);
            
        }  
    }

    private static async void ErabiltzaileaKudeatu(TcpClient socketCliente)
    {
        StreamReader reader = null;
        StreamWriter writer = null;
        BezeroKonektatua bezeroaLogeatu = null;
        
        try
        {
            NetworkStream ns = socketCliente.GetStream();
            writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
            reader = new StreamReader(ns, Encoding.UTF8);
            
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] mezuarenzatiak = line.Split(':');

                /*
                 * mezuaren zatiak: 
                 * LOGIN:erabiltzailea:pasahitza
                */
                string agindua = mezuarenzatiak[0];
                string erabiltzailea = mezuarenzatiak.Length > 1 ? mezuarenzatiak[1] : "";
                string pasahitza = mezuarenzatiak.Length > 2 ? mezuarenzatiak[2] : "";


                if (agindua == "LOGIN" && mezuarenzatiak.Length == 3)
                {
                    bool exists = await databaseOperations.checkErabiltzaileak(erabiltzailea, pasahitza);
                    if (exists)
                    {
                        string[] logeatutakoBezeroak = null;
                        lock (lockObject)
                        {
                            logeatutakoBezeroak = bezeroak.Select(b => b.Erabiltzailea).ToArray();
                        }
                        if (logeatutakoBezeroak.Contains(erabiltzailea))
                        {
                            writer.WriteLine("LOGIN_FAIL:Erabiltzailea jadanik logeatuta dago");
                            continue;
                        }
                        bool isAdmin = await databaseOperations.checkAdmin(erabiltzailea);
                        writer.WriteLine($"LOGIN_OK:{(isAdmin ? "admin" : "user")}");
                        //erabiltzaile motaren arabera ekintza ezberdinak egin
                        if (isAdmin)
                        {
                            Console.WriteLine($"ADMIN erabiltzailea logeatuta: {erabiltzailea}");
                            // bezeroa administratzaile bada admin bezala gorde bezeroen zerrendan
                            bezeroaLogeatu = new BezeroKonektatua
                            {
                                Erabiltzailea = erabiltzailea,
                                SocketCliente = socketCliente,
                                Mota = "admin",
                                Writer = writer,
                                Reader = reader
                            };
                            lock (lockObject)
                            {
                                bezeroak.Add(bezeroaLogeatu);
                            }
                        }
                        else
                        {
                            // bezeroa erabiltzaile arrunta bada user bezala gorde bezeroen zerrendan
                            Console.WriteLine($"USER erabiltzailea logeatuta: {erabiltzailea}");
                            bezeroaLogeatu = new BezeroKonektatua
                            {
                                Erabiltzailea = erabiltzailea,
                                SocketCliente = socketCliente,
                                Mota = "user",
                                Writer = writer,
                                Reader = reader
                            };
                            lock (lockObject)
                            {
                                bezeroak.Add(bezeroaLogeatu);
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine("LOGIN_FAIL:Erabiltzailea edo pasahitza okerra, badaukazu erabiltzailerik?");
                    }
                }
                else if (agindua == "SIGNUP" && mezuarenzatiak.Length == 3)
                {
                    databaseOperations.sortuErabiltzailea(erabiltzailea, pasahitza);
                    Console.WriteLine($"SIGNUP_OK:{erabiltzailea},{pasahitza}");
                    writer.WriteLine("SIGNUP_OK");
                }
                else if (agindua == "GET_USERS" && bezeroaLogeatu != null && bezeroaLogeatu.Mota == "admin")
                {
                    // Administratzaileak erabiltzaile zerrenda eskatu du
                    Console.WriteLine($"Admin {bezeroaLogeatu.Erabiltzailea} erabiltzaile zerrenda eskatzen");
                    List<Erabiltzaile> erabiltzaileak = databaseOperations.kargatuErabiltzaileak();
                    
                    // Bidali erabiltzaile guztiak mezu bakarrean
                    string erabiltzaileZerrenda = string.Join(",", erabiltzaileak);
                    writer.WriteLine($"USERS_LIST:{erabiltzaileZerrenda}");
                    Console.WriteLine($"Erabiltzaile zerrenda bidalita: {erabiltzaileak.Count} erabiltzaile");
                }
                else if (agindua == "DISCONNECT")
                {
                    Console.WriteLine($"Bezeroak deskonexioa eskatu du");
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
            // Kendu bezeroa zerrendatik
            if (bezeroaLogeatu != null)
            {
                lock (lockObject)
                {
                    bezeroak.Remove(bezeroaLogeatu);
                    Console.WriteLine($"Bezeroa {bezeroaLogeatu.Erabiltzailea} zerrendatik kenduta");
                }
            }
            
            reader?.Close();
            writer?.Close();
            socketCliente?.Close();
            Console.WriteLine("Bezeroa deskonektatuta");
        }
    }
}
