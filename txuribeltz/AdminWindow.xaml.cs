using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Windows;
using txuribeltz_server;
using IOPath = System.IO.Path;
namespace txuribeltz
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private StreamWriter writer;
        private StreamReader reader;
        List<Erabiltzaile> erabiltzaileak = new List<Erabiltzaile>(); // Erabiltzaile objetua erabiliko da, bakarrik administratzailearentzako sortu dena
        public DateTime Today { get; set; } // "gaurko data"
        public DateTime LastWeek { get; set; } // defektuz azkeneko 7 eguneko data erakutsiko da

        public AdminWindow(StreamReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
            Today = DateTime.Now.Date;
            LastWeek = DateTime.Now.AddDays(-7).Date;
            InitializeComponent();
            this.DataContext = this;

            // Zerbitzariaren mezuak entzuten egon
            HasiMezuakEntzuten();

            // Eskatu zerbitzariari erabiltzaileen informazioa, erakutsi eta editatu ahal izateko
            erakutsiErabiltzaileak();
        }

        // Zerbitzaritik mezuak entzuten egongo da denbora guztian
        // Jasotzen dituen mezuak prozesatuko ditu agindu ezberdinak kudeatzeko
        private void HasiMezuakEntzuten()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            prozesatuMezua(line);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Konexioa galdu da: {ex.Message}");
                        this.Close();
                    });
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        // Zerbitzaritik datorren mezua prozesatuko duen metodoa.
        // Mezuak "KOMANDOA:arg1:arg2:..." formatuan etorriko direla suposatzen da.
        // Salbuespena: "USERS_LIST:" mezua, bertan payload-a luzea izan daiteke eta ":" karakterea behin bakarrik zatitzea interesatzen zaigu.
        private void prozesatuMezua(string mezua)
        {
            // Mezu hutsak edo null badaude, ez dugu ezer egingo
            if (string.IsNullOrWhiteSpace(mezua))
                return;

            // USERS_LIST kasua berezia da:
            // - Formatoa: USERS_LIST:user1|mota1|pass1;user2|mota2|pass2
            // - ":" karakterea behin bakarrik zatitzea nahi dugu (Split(...,2))
            if (mezua.StartsWith("USERS_LIST:", StringComparison.Ordinal))
            {
                KudeatuUsersList(mezua);
                return;
            }

            // Beste mezu guztietan, protokoloa honakoa da:
            // KOMANDOA:informazioa:informazioa...
            string[] zatiak = mezua.Split(':');

            // Segurtasunagatik: Split-ak array hutsa itzuli dezake egoera arraroetan
            if (zatiak.Length == 0)
                return;

            string komandoa = zatiak[0];

            // Server.cs-en antzera: komandoaren arabera ekintza bat edo beste
            switch (komandoa)
            {
                case "TOP10":
                    KudeatuTop10(zatiak);
                    break;

                case "DATA":
                    KudeatuErabiltzaileDatuak(zatiak);
                    break;

                case "COUNT_PARTIDAK":
                    KudeatuPartidaKopurua(zatiak);
                    break;

                default:
                    // Beste mezu mota batzuk baldin badaude, hemen gehitu daitezke.
                    break;
            }
        }

        // USERS_LIST mezua kudeatzen duen metodoa.
        // Helburua: erabiltzaileak listan/taulan kargatu eta combo-a eguneratu.
        private void KudeatuUsersList(string mezua)
        {
            // Split(...,2): "USERS_LIST" eta informazioa (erabiltzaile guztiak) banatzeko,
            // informazioaren barruan ":" agertuz gero ez puskatzeko.
            string[] mezuarenzatiak = mezua.Split(new[] { ':' }, 2);

            // Informaziorik ez badago edo hutsik badago, zerrendak garbitu eta abisatu
            if (mezuarenzatiak.Length <= 1 || string.IsNullOrWhiteSpace(mezuarenzatiak[1]))
            {
                erabiltzaileak.Clear();
                dgUsers.ItemsSource = null;

                // ez dugu combo-a betetzen daturik ez badago
                comboErabiltzaileak.Items.Clear();

                MessageBox.Show("Ez dago erabiltzailerik.");
                return;
            }

            // Erabiltzaileen zerrenda berriro kargatuko dugu, beraz lehenengo garbitu
            erabiltzaileak.Clear();

            // GET_USERS berriz jasotzean izenak bikoiztuta agertzen dira comboan beraz garbitu
            comboErabiltzaileak.Items.Clear();

            // Erabiltzaile bakoitza ';' karakterearekin dator bananduta
            string[] userEntries = mezuarenzatiak[1].Split(';');

            foreach (string userEntry in userEntries)
            {
                // Sarrera hutsak saltatu
                if (string.IsNullOrWhiteSpace(userEntry))
                    continue;

                // userEntry formatoa: username|mota|password
                string[] userData = userEntry.Split('|');

                // Gutxienez 3 eremu behar dira (izena, mota, pasahitza)
                if (userData.Length < 3)
                    continue;

                // Erabiltzaile objektua sortu
                var erabiltzaile = new Erabiltzaile
                {
                    Erabiltzailea = userData[0].Trim(),
                    Mota = userData[1].Trim(),
                    Pasahitza = userData[2].Trim()
                };

                // Zerrendara gehitu (DataGrid-erako)
                erabiltzaileak.Add(erabiltzaile);

                // ComboBox-era erabiltzaileen izenak bakarrik gehitzen ditugu (adminak aukeratzeko)
                comboErabiltzaileak.Items.Add(erabiltzaile.Erabiltzailea);
            }

            // DataGrid-a eguneratzeko modu erraza: ItemsSource null eta berriro esleitu
            dgUsers.ItemsSource = null;
            dgUsers.ItemsSource = erabiltzaileak;
        }

        // TOP10 mezua kudeatzen duen metodoa.
        // Formatoa: TOP10:user|elo;user2|elo2;...
        private void KudeatuTop10(string[] mezuarenzatiak)
        {
            // Gutxienez TOP10 eta payload-a behar ditugu
            if (mezuarenzatiak.Length < 2)
                return;

            // zatiak[1] = "user|elo;user2|elo2;..."
            List<string> top = mezuarenzatiak[1].Split(';').ToList();

            // PDF-a sortu eta ireki
            exportTo10(top);
        }

        // DATA mezua kudeatzen duen metodoa (erabiltzaile baten estatistikak).
        // Formatoa: DATA:username:elo:wins:losses:winrate
        private void KudeatuErabiltzaileDatuak(string[] mezuarenzatiak)
        {
            // Gutxienez 6 zati behar ditugu (DATA, username, elo, wins, losses, winrate)
            if (mezuarenzatiak.Length < 6)
                return;

            // Server-etik datorren ordena mantendu
            string erabiltzailea = mezuarenzatiak[1];
            string elo = mezuarenzatiak[2];
            string irabaziak = mezuarenzatiak[3];
            string galduak = mezuarenzatiak[4];
            string winrate = mezuarenzatiak[5];

            // PDF-a sortu eta ireki
            exportErabiltzailePDF(erabiltzailea, elo, irabaziak, galduak, winrate);
        }

        // COUNT_PARTIDAK mezua kudeatzen duen metodoa (data tarte bateko partida kopurua).
        // Formatoa: COUNT_PARTIDAK:123
        private void KudeatuPartidaKopurua(string[] mezuarenzatiak)
        {
            // Gutxienez COUNT_PARTIDAK eta kopurua behar dugu
            if (mezuarenzatiak.Length < 2)
                return;

            // zatiak[1] = partida kopurua
            string partidaKopurua = mezuarenzatiak[1];

            // DataPicker-eko datak hartu, PDF-an tartea ondo erakusteko
            // (Server-etik ez badatoz datak bueltan, behintzat UI-tik hartu eta erakutsi)
            string datahasiera = datePickerHasi.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            string dataamaiera = datePickerBukatu.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

            // PDF-a sortu eta ireki
            exportPartidaKopuruaPDF(partidaKopurua, datahasiera, dataamaiera);
        }

        // erabiltzaileak zerrendan kargatu
        private void erakutsiErabiltzaileak()
        {
            try
            {
                // Request users list from server
                writer.WriteLine("GET_USERS");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea erabiltzaileak eskatzean: {ex.Message}");
            }
        }

        // metodo berdina baina botoitik kargatzeko berriro badaezpa ere
        private void erakutsiErabiltzaileak(object sender, RoutedEventArgs e)
        {
            try
            {
                writer.WriteLine("GET_USERS");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea erabiltzaileak eskatzean: {ex.Message}");
            }
        }

        // Zerbitzaritik deskonexioa eskatzen da eta lehioa ixten da
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            writer.WriteLine("DISCONNECT");
            this.Close();
        }

        //aukeratutako erabiltzailearen pasahitza aldatzea eskatu zerbitzariari
        private void pasahitzaAldatu(object sender, RoutedEventArgs e)
        {
            try
            {
                var erabiltzaileAukeratua = dgUsers.SelectedItem as Erabiltzaile;

                if (erabiltzaileAukeratua == null)
                {
                    MessageBox.Show("Aukeratu erabiltzaile bat lehenik.");
                    return;
                }

                // Ireki pasahitza aldatzeko lehioa
                PasahitzaAldatu lehioa = new PasahitzaAldatu();
                bool? emaitza = lehioa.ShowDialog();

                // Egiaztatu erabiltzaileak OK sakatu duen eta pasahitza sartu duen
                if (emaitza == true && !string.IsNullOrWhiteSpace(lehioa.PasahitzaBerria))
                {
                    string izena = erabiltzaileAukeratua.Erabiltzailea;
                    string pasahitzaBerria = lehioa.PasahitzaBerria;

                    // Bidali pasahitza aldatzeko mezua zerbitzariari
                    writer.WriteLine($"CHANGE_P:{izena}:{pasahitzaBerria}");

                    // Itxaron pixka bat eta freskatu erabiltzaileen zerrenda
                    Thread.Sleep(500);
                    erakutsiErabiltzaileak();

                    //MessageBox.Show($"{izena} erabiltzailearen pasahitza aldatu da.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea pasahitza aldatzean: {ex.Message}");
            }
        }

        //aukeratutako erabiltzailea ezabatzea eskatu zerbitzariari
        private void ezabatuErabiltzailea(object sender, RoutedEventArgs e)
        {
            try
            {
                var erabiltzaileAukeratua = dgUsers.SelectedItem as Erabiltzaile;

                if (erabiltzaileAukeratua == null)
                {
                    MessageBox.Show("Aukeratu erabiltzaile bat lehenik.");
                    return;
                }

                string izena = erabiltzaileAukeratua.Erabiltzailea;

                // Baieztapen mezua erakutsi
                var emaitza = MessageBox.Show(
                    $"Ziur zaude {izena} erabiltzailea ezabatu nahi duzula?",
                    "Baieztapena",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (emaitza == MessageBoxResult.Yes)
                {
                    // Bidali ezabatzeko mezua zerbitzariari
                    writer.WriteLine($"DELETE:{izena}");

                    // Itxaron pixka bat eta eguneratu erabiltzaileen zerrenda
                    Thread.Sleep(500);
                    erakutsiErabiltzaileak();

                    MessageBox.Show($"{izena} erabiltzailea ezabatu da.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea erabiltzailea ezabatzean: {ex.Message}");
            }
        }

        // Daten arteko partida kopurua eskatzea zerbitzariari
        private void dataStatistikak_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Aukeratutako datak hartu eta egiaztatu
                string datahasiera = datePickerHasi.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
                string dataamaiera = datePickerBukatu.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

                if (datePickerHasi.SelectedDate > datePickerBukatu.SelectedDate)
                {
                    MessageBox.Show("Hasierako data ezin da amaierako data baina beranduagoa izan.");
                    return;
                }
                MessageBox.Show($"Data hasiera: {datahasiera}\nData amaiera: {dataamaiera}");
                // zerbitzariari data estatistikak eskatzea
                // Datubaseko data formatua "2026-01-16" izango da
                writer.WriteLine($"GET_DATA_STATS:{datahasiera}:{dataamaiera}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea data estatistikak eskatzean: {ex.Message}");
            }
        }

        // TOP 10 jokalariak kargatu zerbitzaritik
        private void top_btn_Click_1(object sender, RoutedEventArgs e)
        {
            kargatuTOP10();
        }

        // Zerbitzariari to 10 jokalarien informazioa eskatuko diogu, beranduago menuan erakutsi ahal izateko, bakarrik izena eta elo-a inporta zaizkigu
        private void kargatuTOP10()
        {
            try
            {
                // zerbitzariari bidali informazioa nahi dugula
                writer.WriteLine($"TOP_10:");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea top 10 informazioa eskatzean: {ex.Message}");
            }
        }

        // zerbitzariari aukeratutako jokalariaren estatistikak eskatzea
        private void jokalariStatistikak_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboErabiltzaileak.SelectedItem == null)
                {
                    MessageBox.Show("Aukeratu erabiltzaile bat lehenik.");
                    return;
                }
                else
                {
                    string izena = comboErabiltzaileak.SelectedItem.ToString();
                    // zerbitzariari jokalariaren estatistikak eskatzea
                    writer.WriteLine($"USERDATA:{izena}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea jokalariaren datuak eskatzean: {ex.Message}");
            }
        }

        // TOP 10 jokalariak PDF batean exportatu edo erakutsi
        private void exportTo10(List<string> erabiltzaileak)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var folder = PdfExport.EnsureOutputFolder();
                var filePath = IOPath.Combine(folder, $"Top10_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                var doc = new Top10Document(erabiltzaileak);
                doc.GeneratePdf(filePath);

                PdfExport.OpenFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea TOP 10 jokalarien PDF-a sortzean: {ex.Message}");
            }
        }

        // Erabiltzaile baten datuak PDF batean exportatu edo erakutsi
        private void exportErabiltzailePDF(string erabiltzailea, string elo, string irabaziak, string galduak, string winrate)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var folder = PdfExport.EnsureOutputFolder();
                var safeUser = string.Join("_", erabiltzailea.Split(IOPath.GetInvalidFileNameChars()));
                var filePath = IOPath.Combine(folder, $"User_{safeUser}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                var doc = new UserStatsDocument(erabiltzailea, elo, irabaziak, galduak, winrate);
                doc.GeneratePdf(filePath);

                PdfExport.OpenFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea {erabiltzailea} erabiltzailearen PDF-a sortzean: {ex.Message}");
            }
        }

        // Partida kopurua exportatu pdf batean
        private void exportPartidaKopuruaPDF(string partidaKopurua, string dataHasiera, string dataAmaiera)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                var folder = PdfExport.EnsureOutputFolder();
                var filePath = IOPath.Combine(folder, $"PartidaKopurua_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                var doc = new PartidaKopuruaDocument(partidaKopurua, dataHasiera, dataAmaiera);
                doc.GeneratePdf(filePath);
                PdfExport.OpenFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea partida kopuruaren PDF-a sortzean: {ex.Message}");
            }
        }
    }
}
