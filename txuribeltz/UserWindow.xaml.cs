using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using txuribeltz_server;

namespace txuribeltz
{
    public partial class UserWindow : Window
    {
        private bool kolanDago = false;
        //private bool partidaAurkituta = false;
        private StreamWriter writer;
        private StreamReader reader;
        private string erabiltzailea;
        private string erabiltzaileaElo;
        private string? currentOpponent;
        private string? currentOpponentElo;
        private bool shouldListen = true;


        public UserWindow(StreamReader reader, StreamWriter writer, string erabiltzailea)
        {
            this.reader = reader;
            this.writer = writer;
            this.erabiltzailea = erabiltzailea;
            InitializeComponent();
            // zerbitzaritik datozen mezuak entzuten egon denbora guztian
            hasiMezuakEntzuten();
            // Logeatu garen erabiltzailearen datuak kargatuko ditugu eta pantaian erakutsiko ditugu
            kargatuErabiltzailea();
            // Menuan, eskubiko atalean dagoen top 10 jokalarien zerrenda kargatuko dugu
            kargatuTOP10();
        }

        // Zerbitzaritik mezuak entzuten egongo da denbora guztian
        // Jasotzen dituen mezuak prozesatuko ditu agindu ezberdinak kudeatzeko
        private void hasiMezuakEntzuten()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    string line;
                    while (shouldListen && (line = reader.ReadLine()) != null)
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
                        MessageBox.Show($"Konexioa galdu da zerbitzariarekin: {ex.Message}");
                        this.Close();
                    });
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        // Gure erabiltzailearen informazioa zerbitzariari eskatuko diogu
        private void kargatuErabiltzailea()
        {
            try
            {
                // eskatu gure erabiltzailearen informazioa
                writer.WriteLine($"USERDATA:");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea erabiltzailearen informazioa eskatzean: {ex.Message}");
            }
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

        // Zerbitzaritik datorren mezua prozesatuko duen metodoa.
        // Agindua bananduko da eta mezua ere, aginduaren arabera ekintza ezberdinak kudeatuko dira.
        private void prozesatuMezua(string mezua)
        {
            // Mezuak formatu hau izango du: AGINDUA:informazioa:informazioa...
            // Zerbitzarian kudeatuko dira aginduak
            string[] mezuarenzatiak = mezua.Split(':');
            string agindua = mezuarenzatiak[0];

            switch (agindua)
            {
                case "DATA":
                        Dispatcher.Invoke(() =>
                        {
                            txt_username.Text = mezuarenzatiak[1].ToUpper();
                            lblUsername.Text = mezuarenzatiak[1];
                            lblElo.Text = mezuarenzatiak[2];
                            erabiltzaileaElo = mezuarenzatiak[2];
                            lblIrabazita.Text = mezuarenzatiak[3];
                            lblGalduta.Text = mezuarenzatiak[4];
                            lblWinRate.Text = $"{mezuarenzatiak[5]}";
                        });
                    break;

                case "MATCH_FOUND":
                    if (mezuarenzatiak.Length >= 3)
                    {
                        currentOpponent = mezuarenzatiak[1];
                        currentOpponentElo = mezuarenzatiak[2];
                        //partidaAurkituta = true;
                        kolanDago = false;

                        Dispatcher.Invoke(() =>
                        {
                            if (!kolanDago)
                            {
                                lblPartidaBilatzen.Visibility = Visibility.Collapsed;
                                lblOpponent.Text = currentOpponent;
                                lblEloAurkaria.Text = currentOpponentElo;
                                partidaInformazioa.Visibility = Visibility.Visible;
                            }
                        });
                    }
                    break;

                case "MATCH_STARTED":
                    Dispatcher.Invoke(() =>
                    {
                        // lehio honetatik ez entzun gehiago zerbitzariari
                        shouldListen = false;

                        // Ireki jokuaren lehioa, bertatik kudeatuko da jokoaren logika guztia eta lehio hau itxi
                        new GameWindow(reader, writer, erabiltzailea, erabiltzaileaElo, currentOpponent, currentOpponentElo).Show();
                        this.Close();
                    });
                    break;

                    case "TOP10":
                    List<string> erabiltzaileak = mezuarenzatiak[1].Split(';').ToList();
                    Dispatcher.Invoke(() =>
                    {
                        topJokalariakPanel.Children.Clear();
                        foreach (string erabiltzailea in erabiltzaileak)
                        {
                            string[] datuak = erabiltzailea.Split('|');
                            TextBlock txtBlock = new TextBlock
                            {
                                Text = $"{datuak[0].ToUpper()} : {datuak[1]}",
                                FontSize = 16,
                                Margin = new Thickness(5),
                                Foreground = Brushes.White
                            };
                            topJokalariakPanel.Children.Add(txtBlock);
                        }
                    });
                    break;

                default:
                    break;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            writer.WriteLine("DISCONNECT");
            this.Close();
        }

        // Pasahitza aldatzeko formularioa erakutsi
        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            editFormPanel.Visibility = Visibility.Visible;
        }
        // Pasahitza aldatzeko formularioa ezkutatu
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            editFormPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Bidali pasahitza aldatzeko mezua zerbitzariari
            writer.WriteLine($"CHANGE_P:{erabiltzailea}:{txtNewPassword.Password}");
            editFormPanel.Visibility = Visibility.Collapsed;
        }

        // Partida bilatunahi dugula bidali zerbitzariari, horrela, zerbitzariak kolan sartuko gaitu aurkalariren bat aurkitzeko
        private void QueueMatch_Click(object sender, RoutedEventArgs e)
        {
            kolanDago = true;
            lblPartidaBilatzen.Visibility = Visibility.Visible;
            partidaInformazioa.Visibility = Visibility.Collapsed;
            writer.WriteLine($"FIND_MATCH:");
        }

        // Partida hasteko prest gaudela adierazi zerbitzariari, hala ere, partida hasiko da bi aurkalarietako batek ematen dionean
        private void StartMatch_Click(object sender, RoutedEventArgs e)
        {
            if (currentOpponent != null)
            {
                writer.WriteLine($"START_MATCH:{currentOpponent}");
            }
            else
            {
                MessageBox.Show("Errorea: Oponentea ez da zehaztu");
            }
        }
    }
}
