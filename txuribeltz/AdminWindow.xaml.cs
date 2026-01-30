using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        // Agindua bananduko da eta mezua ere, aginduaren arabera ekintza ezberdinak kudeatuko dira.
        private void prozesatuMezua(string mezua)
        {
            // Egiaztatu mezua USERS_LIST motakoa den
            if (mezua.StartsWith("USERS_LIST:"))
            {
                // Parseatu erabiltzaile lista: USERS_LIST:user1|mota1|pass1;user2|mota2|pass2

                // 1. Zatitu mezua lehenengo ':' karakterean, gehienez 2 zatitan
                //    parts[0] = "USERS_LIST" (protokoloaren komandoa)
                //    parts[1] = "user1|mota1|pass1;user2|mota2|pass2" (erabiltzaileen datuak)
                string[] parts = mezua.Split(new[] { ':' }, 2);

                // 2. Egiaztatu bigarren zatia existitzen dela eta ez dagoela hutsik
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    // 3. Garbitu erabiltzaileen zerrenda lehendik dauden erabiltzaileak ezabatzeko
                    erabiltzaileak.Clear();

                    // 4. Zatitu erabiltzaileen datuak ';' karakterearen bidez
                    //    Erabiltzaile bakoitza ';' karaktereaz bereizita dago
                    //    Adib: ["user1|mota1|pass1;user2|mota2|pass2"]
                    string[] userEntries = parts[1].Split(';');

                    // 5. Erabiltzaile bakoitza prozesatu
                    foreach (string userEntry in userEntries)
                    {
                        // Saltatu sarrera hutsak
                        if (string.IsNullOrWhiteSpace(userEntry))
                            continue;

                        // 6. Zatitu erabiltzaile bakoitzaren datuak '|' karakterearen bidez
                        //    userData[0] = erabiltzaile izena
                        //    userData[1] = mota (admin edo user)
                        //    userData[2] = pasahitza
                        string[] userData = userEntry.Split('|');

                        // 7. Egiaztatu 3 datu daudela gutxienez
                        if (userData.Length >= 3)
                        {
                            // 8. Sortu Erabiltzaile objektu berria eta gehitu zerrendara
                            erabiltzaileak.Add(new Erabiltzaile
                            {
                                Erabiltzailea = userData[0].Trim(), // Kendu zuriuneak hasieran eta amaieran
                                Mota = userData[1].Trim(),
                                Pasahitza = userData[2].Trim()
                            });
                            // Gehitu erabiltzaile izena combo box-era
                            comboErabiltzaileak.Items.Add(userData[0].Trim());
                        }
                    }

                    // 9. Eguneratu DataGrid-a erabiltzaile berrien zerrendarekin
                    //    Lehenengo null jarri eta gero zerrenda berria esleitu DataGrid-a behar bezala freskatzeko
                    dgUsers.ItemsSource = null;
                    dgUsers.ItemsSource = erabiltzaileak;
                }
                else
                {
                    // Ez dago daturik mezuan
                    erabiltzaileak.Clear();
                    dgUsers.ItemsSource = null;
                    MessageBox.Show("Ez dago erabiltzailerik.");
                }
            } else if (mezua.StartsWith("TOP10:"))
            {
                string[] mezuarenzatiak = mezua.Split(":");
                List<string> erabiltzaileak = mezuarenzatiak[1].Split(';').ToList();
                // TOP 10 jokalariak PDF batean exportatu edo erakutsi
                exportTo10(erabiltzaileak);
            } else if (mezua.StartsWith("DATA:"))
            {
                string[] mezuarenzatiak = mezua.Split(":");

                string erabiltzailea = mezuarenzatiak[1];
                string elo = mezuarenzatiak[2];
                string irabaziak = mezuarenzatiak[3];
                string galduak = mezuarenzatiak[4];
                string winrate = $"{mezuarenzatiak[5]}";
                // Erabiltzailearen datuak erakutsi PDF batean
                exportErabiltzailePDF(erabiltzailea, elo, irabaziak, galduak, winrate);
            } else if (mezua.StartsWith("COUNT_PARTIDAK")){
                string[] mezuarenzatiak = mezua.Split(":");
                string partidaKopurua = mezuarenzatiak[1];
                string datahasiera = datePickerHasi.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
                string dataamaiera = datePickerBukatu.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
                exportPartidaKopuruaPDF(partidaKopurua, datahasiera, dataamaiera);
            }
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
