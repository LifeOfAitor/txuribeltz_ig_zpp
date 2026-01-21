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
            hasiMezuakEntzuten();
            kargatuErabiltzailea();
            kargatuTOP10();
        }

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
        private void kargatuTOP10()
        {
            try
            {
                // eskatu gure erabiltzailearen informazioa
                writer.WriteLine($"TOP_10:");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea top 10 informazioa eskatzean: {ex.Message}");
            }
        }

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

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            editFormPanel.Visibility = Visibility.Visible;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            // Bidali pasahitza aldatzeko mezua zerbitzariari
            writer.WriteLine($"CHANGE_P:{erabiltzailea}:{txtNewPassword.Password}");
            editFormPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            editFormPanel.Visibility = Visibility.Collapsed;
        }

        private void QueueMatch_Click(object sender, RoutedEventArgs e)
        {
            kolanDago = true;
            lblPartidaBilatzen.Visibility = Visibility.Visible;
            // Bidaltzen da elo-a zerbitzariari (zerbitzariak daturik esleituko du)
            writer.WriteLine($"FIND_MATCH:");
        }

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
