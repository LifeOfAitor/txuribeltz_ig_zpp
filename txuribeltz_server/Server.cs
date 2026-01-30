using Npgsql.Internal;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using txuribeltz_server;


// Zerbitzariaren klase nagusia. TCP konexioak kudeatzen ditu eta bezeroei zerbitzuak eskaintzen dizkie.

public class Server
{
    private static readonly object lockObject = new();
    // Konektatutako bezeroen zerrenda (zerbitzarian bai baina autentikatu gabe)
    private static readonly List<BezeroKonektatuaDatuBasean> zerbitzarikoBezeroak = [];
    // gehienezko bezero kopurua
    private const int MaxBezeroak = 10;
    // Kolan itxaroten dauden bezeroak
    private static readonly List<BezeroKonektatuaDatuBasean> kolanDaudenErabiltzaileak = [];
    // Martxan dauden partiden Diktionarioa, bertan Partida objektuak gordeko dira
    private static readonly Dictionary<string, Partida> partidaAktiboak = new();

    // zerbitzariaren IP helbidea lortzeko metodoa 100% IA egina
    static string GetLocalIPAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip.Address))
                {
                    return ip.Address.ToString();
                }
            }
        }

        throw new Exception("Ez da aurkitu IP-a konpotik konektatzeko");
    }

    // Zerbitzariaren metodo nagusia, zerbitzaria hasi eta bezeroak onartzen eta kudeatzen dituena
    public static async Task Main(string[] args)
    {
        // Zerbitzariaren datuak
        string servidor = "127.0.0.1";
        string servidorV2 = GetLocalIPAddress();
        IPAddress ipserver = IPAddress.Parse(servidor);
        int port = 13000;

        // TCP listener sortu eta martxan jarri
        // LOCALHOST
        //TcpListener listener = new(ipserver, port);
        // LOCALHOST baina edozein IP-tik konektatu ahal izateko
        TcpListener listener = new(IPAddress.Any, port);
        //Console.WriteLine("Zerbitzaria martxan dago {0}:{1}", servidor, port);
        Console.WriteLine($"Zerbitzaria martxan dago {port} portuan");
        Console.WriteLine($"Bezeroa ordenagailu honetan badago konektatu IP helbide honekin:              {servidor}");
        Console.WriteLine($"Bezeroa sareko beste ordenagailu batean badago konektatu IP helbide honekin:  {servidorV2}");
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
                    if (zerbitzarikoBezeroak.Count >= MaxBezeroak)
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
        // bezeroaren reader, writer sortu. 
        StreamReader? reader = null;
        StreamWriter? writer = null;
        BezeroKonektatuaDatuBasean? logeatutakoBezeroa = null;

        try
        {
            //NetworkStream sortu, reader ezarri, writer ezarri
            NetworkStream ns = socketCliente.GetStream();
            writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
            reader = new StreamReader(ns, Encoding.UTF8);

            //line izango da bezeroaren mezua
            string? line;

            // denbora guztian egongo da entzuten zerbitzaria bezeroaren mezuei
            // 'await' erabilita haria blokeatu gabe itxaroten egongo da mezua iritsi arte
            // 'async' erabiliko da UI blokeatuta ez geratzeko
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // mezua komandoak izango dira adibidez:
                // LOGIN:erabiltzailea:pasahitza
                string[] mezuarenzatiak = line.Split(':');
                // agindua gordeko dugu eta horren arabera metodo bat edo bestea erabiliko dugu
                string agindua = mezuarenzatiak[0];

                // Bezeroaren komandoa asinkronoki prozesatzen du eta saioaren egoera (logeatutako bezeroa) eguneratzen du; 
                // 'await' erabilita haria blokeatu gabe itxaroten da emaitza lortu arte.
                // Bezeroaren informazioa beteko da behin logeatzen garenean 
                logeatutakoBezeroa = await ProzesatuAgindua(agindua, mezuarenzatiak, writer, reader, socketCliente, logeatutakoBezeroa);

                // bezeroak deskonexioa eskatzen duenean bukletik aterako gara
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
            // Bezeroa deskonektatu eta garbitu baliabideak
            KenduBezeroa(logeatutakoBezeroa);
            KenduKolatik(logeatutakoBezeroa);
            reader?.Close();
            writer?.Close();
            socketCliente?.Close();
            Console.WriteLine("Bezeroa deskonektatuta");
        }
    }

    // bezeroak bidalitako agindua prozesatuko da metodo honetan switch-case baten bidez
    private static async Task<BezeroKonektatuaDatuBasean?> ProzesatuAgindua(
        string agindua,
        string[] mezuarenzatiak,
        StreamWriter writer,
        StreamReader reader,
        TcpClient socketCliente,
        BezeroKonektatuaDatuBasean? logeatutakoBezeroa)
    {
        // case luzeak beste metodo batzuetan banandu ditut kodearen egokiera mantentzeko
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

            case "USERDATA":
                userdataKudeatu(logeatutakoBezeroa, mezuarenzatiak, writer);
                break;

            case "GET_DATA_STATS":
                DateTime hasieradata = DateTime.Parse(mezuarenzatiak[1]);
                DateTime bukaeradata = DateTime.Parse(mezuarenzatiak[2]);
                string? statsEmaitza = databaseOperations.partidaKopuruaLortu(hasieradata, bukaeradata);
                writer.WriteLine($"COUNT_PARTIDAK:{statsEmaitza}");
                break;


            case "TOP_10":
                List<string> erabiltzaileak = databaseOperations.lortuTOP10();

                string mezuaTop10 = string.Join(";", erabiltzaileak);

                //Console.WriteLine($"DEBUG TOP10:{mezuaTop10}");
                writer.WriteLine($"TOP10:{mezuaTop10}");

                break;

            case "FIND_MATCH":
                findMatchKudeatu(logeatutakoBezeroa, writer);
                break;

            case "START_MATCH":
                startMatchKudeatu(logeatutakoBezeroa, mezuarenzatiak, writer);
                break;

            // Txat mezua jaso eta partida objektuari pasatu mezua kudeatzeko
            case "CHAT":
                if (logeatutakoBezeroa != null && mezuarenzatiak.Length >= 2)
                {
                    string mezua = string.Join(":", mezuarenzatiak[1]);
                    var partida = LortuBezeroPartida(logeatutakoBezeroa);

                    if (partida != null)
                    {
                        // Partida objektuak kudeatzen du
                        partida.ProzesatuChatMezua(logeatutakoBezeroa.Erabiltzailea, mezua);
                    }
                    else
                    {
                        //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} ez dago partidarik");
                        writer.WriteLine("ERROR:Ez zaude partidarik jolasten");
                    }
                }
                break;

            case "MOVE":
                if (logeatutakoBezeroa != null && mezuarenzatiak.Length >= 2)
                {
                    // MOVE:row,col (adibidez: MOVE:7,7)
                    string[] posizioa = mezuarenzatiak[1].Split(',');
                    if (posizioa.Length == 2 &&
                        int.TryParse(posizioa[0], out int row) &&
                        int.TryParse(posizioa[1], out int col))
                    {
                        var partida = LortuBezeroPartida(logeatutakoBezeroa);

                        if (partida != null)
                        {
                            // Partida objektuak kudeatzen du
                            partida.ProzesatuMugimendua(logeatutakoBezeroa.Erabiltzailea, row, col);
                        }
                        else
                        {
                            //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} ez dago partidarik");
                            writer.WriteLine("ERROR:Ez zaude partidarik jolasten");
                        }
                    }
                    else
                    {
                        writer.WriteLine("ERROR:Mugimendua formatu okerra");
                    }
                }
                break;

            case "WIN":
                if (logeatutakoBezeroa != null)
                {
                    var partida = LortuBezeroPartida(logeatutakoBezeroa);
                    if (partida != null)
                    {
                        string? irabazlea = mezuarenzatiak[1];
                        partida.AmaituPartida(irabazlea);
                        // bidali irabazleari mezua eta kendu partida

                        KenduPartida(partida.PartidaID);
                        //Console.WriteLine($"DEBUG: partida bukatuta eta ezabatuta");
                    }
                }
                break;

            case "LEAVE_MATCH":
                if (logeatutakoBezeroa != null)
                {
                    var partida = LortuBezeroPartida(logeatutakoBezeroa);
                    if (partida != null)
                    {
                        // Utzi = galdu
                        string? irabazlea = partida.LortuAurkalaria(logeatutakoBezeroa.Erabiltzailea);
                        partida.AmaituPartida(irabazlea);
                        KenduPartida(partida.PartidaID);
                        //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} partida utzi du");
                    }
                }
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

    private static void startMatchKudeatu(BezeroKonektatuaDatuBasean logeatutakoBezeroa, string[] mezuarenzatiak, StreamWriter writer)
    {
        //Console.WriteLine($"DEBUG: START_MATCH jaso - mezuarenzatiak.Length: {mezuarenzatiak.Length}");
        if (logeatutakoBezeroa != null && mezuarenzatiak.Length >= 2)
        {
            string oponentea = mezuarenzatiak[1];
            var aurkalaria = zerbitzarikoBezeroak.FirstOrDefault(b => b.Erabiltzailea == oponentea);

            if (aurkalaria != null)
            {
                // Sortu PartidaId name1_name2 eran (alfabetikoki ordenatuta)
                var izenak = new[] { logeatutakoBezeroa.Erabiltzailea, oponentea };
                Array.Sort(izenak);
                string partidaID = $"{izenak[0]}_{izenak[1]}";

                lock (lockObject)
                {
                    // Egiaztatu partida ez dagoela jadanik
                    if (partidaAktiboak.ContainsKey(partidaID))
                    {
                        //Console.WriteLine($"DEBUG: Partida {partidaID} dagoeneko existitzen da");
                        writer.WriteLine("ERROR:Partida dagoeneko hasita dago");
                        return;
                    }

                    // Sortu partida berria
                    var partida = new Partida(partidaID, logeatutakoBezeroa, aurkalaria);
                    partidaAktiboak[partidaID] = partida;

                    // Gorde PartidaID bi bezeroetan
                    logeatutakoBezeroa.PartidaID = partidaID;
                    aurkalaria.PartidaID = partidaID;

                    Console.WriteLine($"PARTIDA SORTU DA -> Partida id:{partidaID} -> {logeatutakoBezeroa} eta {aurkalaria}");
                }

                // Bidali biei partida hasi dela
                partidaAktiboak[partidaID].BidaliBieiei($"MATCH_STARTED:{partidaID}");
                //Console.WriteLine($"DEBUG: MATCH_STARTED bidalita bieei - {partidaID}");
            }
            else
            {
                //Console.WriteLine($"DEBUG: ERROREA - Aurkalaria {oponentea} ez da aurkitu zerbitzarian");
                writer.WriteLine("ERROR:Aurkalaria ez da aurkitu");
            }
        }
        else
        {
            Console.WriteLine($"DEBUG: START_MATCH - parametro falta edo bezeroa null");
        }
    }

    private static void findMatchKudeatu(BezeroKonektatuaDatuBasean logeatutakoBezeroa, StreamWriter writer)
    {
        if (logeatutakoBezeroa != null)
        {
            //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} partida bilatzen");
            GehituKolara(logeatutakoBezeroa);

            // Bilatu aurkalaria
            var aurkalaria = AurkituAurkalaria(logeatutakoBezeroa);
            if (aurkalaria != null)
            {
                //Console.WriteLine($"DEBUG: Partidua aurkitu - {logeatutakoBezeroa.Erabiltzailea} vs {aurkalaria.Erabiltzailea}");

                // Lortu aurkalariaren datuak
                string? aurkalariElo = databaseOperations.eloLortu(aurkalaria.Erabiltzailea);
                // Lortu logeatuaren datuak
                string? logeatuaElo = databaseOperations.eloLortu(logeatutakoBezeroa.Erabiltzailea);

                if (!string.IsNullOrEmpty(aurkalariElo) && !string.IsNullOrEmpty(logeatuaElo))
                {
                    // Bidali aurkalariaren datuak logeatuari
                    writer.WriteLine($"MATCH_FOUND:{aurkalaria.Erabiltzailea}:{aurkalariElo}");
                    //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} jakin dezan aurkalaria: {aurkalaria.Erabiltzailea}");

                    // Bidali logeatuaren datuak aurkariari
                    aurkalaria.Writer.WriteLine($"MATCH_FOUND:{logeatutakoBezeroa.Erabiltzailea}:{logeatuaElo}");
                    //Console.WriteLine($"DEBUG: {aurkalaria.Erabiltzailea} jakin dezan aurkalaria: {logeatutakoBezeroa.Erabiltzailea}");
                }

                // Kendu biak kolatik partida aurkitu dutelako
                KenduKolatik(logeatutakoBezeroa);
                KenduKolatik(aurkalaria);
            }
            else
            {
                //Console.WriteLine($"DEBUG: {logeatutakoBezeroa.Erabiltzailea} itxaroten, aurkalaririk ez");
            }
        }
    }

    private static void userdataKudeatu(BezeroKonektatuaDatuBasean logeatutakoBezeroa, string[] mezuarenzatiak, StreamWriter writer)
    {
        // banandu behar da, datuak erabiltzaileak eskatzen dituenean edo adminak beste erabiltzaile baten datuak eskatzen dituenean
        if (logeatutakoBezeroa.Mota == "admin" && mezuarenzatiak.Length == 2)
        {
            // adminak beste erabiltzaile baten datuak eskatzen ditu
            string erabiltzailea = mezuarenzatiak[1];
            string? emaitza = databaseOperations.lortuBezeroInformazioaMenurako(erabiltzailea);
            if (!string.IsNullOrEmpty(emaitza))
            {
                writer.WriteLine(emaitza); //DATA:erabiltzailea:elo:partidakJokatu:partidakIrabazi:winrate
            }
            else
            {
                Console.WriteLine($"ERROR:{erabiltzailea} datuak ez dira aurkitu");
            }
        }
        else
        {
            // erabiltzaileak bere datuak eskatzen ditu
            string? emaitza = databaseOperations.lortuBezeroInformazioaMenurako(logeatutakoBezeroa.Erabiltzailea);

            if (!string.IsNullOrEmpty(emaitza))
            {
                writer.WriteLine(emaitza); //DATA:erabiltzailea:elo:partidakJokatu:partidakIrabazi:winrate
            }
            else
            {
                Console.WriteLine($"ERROR:{logeatutakoBezeroa.Erabiltzailea} datuak ez dira aurkitu");
            }
        }
    }

    // Bezeroa datu basean autentikatzen du eta konprobazioak egiten ditu, dena ondo badago,
    // bezeroa logeatuta itzultzen du
    private static async Task<BezeroKonektatuaDatuBasean?> LoginKudeatu(

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

        var bezeroBerria = new BezeroKonektatuaDatuBasean
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

    // Bezero berria sortzen du datu basean
    private static Task SignupKudeatu(string[] mezuarenzatiak, StreamWriter writer)
    {
        string erabiltzailea = mezuarenzatiak[1];
        string pasahitza = mezuarenzatiak[2];

        databaseOperations.sortuErabiltzailea(erabiltzailea, pasahitza);
        Console.WriteLine($"SIGNUP_OK:{erabiltzailea}");
        writer.WriteLine("SIGNUP_OK");

        return Task.CompletedTask;
    }

    // Admin erabiltzaileak erabiltzaileen zerrenda eskatzen duenean, datu baseko erabiltzaileak kargatu eta bidaltzen ditu
    private static void GetUsersKudeatu(BezeroKonektatuaDatuBasean logeatutakoBezeroa, StreamWriter writer)
    {
        Console.WriteLine($"Admin: {logeatutakoBezeroa.Erabiltzailea} erabiltzaile zerrenda eskatzen");
        List<Erabiltzaile> erabiltzaileak = databaseOperations.kargatuErabiltzaileak();

        string erabiltzaileZerrenda = string.Join(";",
            erabiltzaileak.Select(u => $"{u.Erabiltzailea}|{u.Mota}|{u.Pasahitza}"));

        writer.WriteLine($"USERS_LIST:{erabiltzaileZerrenda}");
        Console.WriteLine($"Erabiltzaile zerrenda bidalita: {erabiltzaileak.Count} erabiltzaile");
    }

    // egiaztatu erabiltzailea jadanik logeatuta dagoen
    private static bool ErabiltzaileaLogeatuta(string erabiltzailea)
    {
        lock (lockObject)
        {
            return zerbitzarikoBezeroak.Any(b => b.Erabiltzailea == erabiltzailea);
        }
    }

    //bezeroa zerbitzarian gehitu
    private static void GehituBezeroa(BezeroKonektatuaDatuBasean bezeroa)
    {
        lock (lockObject)
        {
            zerbitzarikoBezeroak.Add(bezeroa);
            Console.WriteLine($"LOGEATUTAKO BEZERO KOPURUA: {zerbitzarikoBezeroak.Count}");
        }
    }

    // Bezeroa zerbitzaritik kendu
    private static void KenduBezeroa(BezeroKonektatuaDatuBasean? bezeroa)
    {
        if (bezeroa == null) return;

        lock (lockObject)
        {
            zerbitzarikoBezeroak.Remove(bezeroa);
            Console.WriteLine($"Bezeroa {bezeroa.Erabiltzailea} zerrendatik kenduta");
            Console.WriteLine($"LOGEATUTAKO BEZERO KOPURUA: {zerbitzarikoBezeroak.Count}");
        }
    }

    // Bezeroa kolara gehitu
    private static void GehituKolara(BezeroKonektatuaDatuBasean bezeroa)
    {
        lock (lockObject)
        {
            if (!kolanDaudenErabiltzaileak.Contains(bezeroa))
            {
                kolanDaudenErabiltzaileak.Add(bezeroa);
                Console.WriteLine($"DEBUG: {bezeroa.Erabiltzailea} kolara gehituta. Kolako kopurua: {kolanDaudenErabiltzaileak.Count}");
            }
        }
    }


    // Kolatik bezeroa kendu
    private static void KenduKolatik(BezeroKonektatuaDatuBasean? bezeroa)
    {
        if (bezeroa == null) return;

        lock (lockObject)
        {
            kolanDaudenErabiltzaileak.Remove(bezeroa);
            //Console.WriteLine($"DEBUG: {bezeroa.Erabiltzailea} kolatik kenduta. Kolako kopurua: {kolanDaudenErabiltzaileak.Count}");
        }
    }

    //Kolan dauden erabiltzaileen artean aurkitu aurkalaria, nilatzen ari den bezeroa ez den beste bat
    private static BezeroKonektatuaDatuBasean? AurkituAurkalaria(BezeroKonektatuaDatuBasean bezeroa)
    {
        lock (lockObject)
        {
            // Bilatu aurkalaria, listan dauden erabiltzaileetatik, baina ez gu
            var opositora = kolanDaudenErabiltzaileak.FirstOrDefault(b => b.Erabiltzailea != bezeroa.Erabiltzailea);
            return opositora;
        }
    }

    // Bezeroaren partida lortu partidaAktiboak diktionarioatik
    private static Partida? LortuBezeroPartida(BezeroKonektatuaDatuBasean bezeroa)
    {
        if (string.IsNullOrEmpty(bezeroa.PartidaID))
        {
            Console.WriteLine($"DEBUG: {bezeroa.Erabiltzailea} ez dauka PartidaId");
            return null;
        }

        lock (lockObject)
        {
            if (partidaAktiboak.TryGetValue(bezeroa.PartidaID, out var partida))
            {
                return partida;
            }
        }

        Console.WriteLine($"DEBUG: Partida {bezeroa.PartidaID} ez da aurkitu partidaAktiboak-en");
        return null;
    }

    // Partida bukatu denean kendu partidaAktiboak diktionarioatik
    private static void KenduPartida(string partidaId)
    {
        lock (lockObject)
        {
            if (partidaAktiboak.Remove(partidaId))
            {
                Console.WriteLine($"DEBUG: Partida kenduta: {partidaId}");
            }
        }
    }
}