using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using txuribeltz_server;

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

        public AdminWindow(StreamReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
            InitializeComponent();

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
    }
}
