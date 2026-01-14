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
        List<Erabiltzaile> erabiltzaileak = new List<Erabiltzaile>();

        public AdminWindow(StreamReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
            InitializeComponent();
            
            // Start listening for server messages in this window
            HasiMezuakEntzuten();
            
            // Request user list from server
            erakutsiErabiltzaileak();
        }

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
                            ProzesatuMezua(line);
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

        private void ProzesatuMezua(string mezua)
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
            // Etorkizunean beste mezu mota gehiago gehitu daitezke hemen (else if...)
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
    }
}
