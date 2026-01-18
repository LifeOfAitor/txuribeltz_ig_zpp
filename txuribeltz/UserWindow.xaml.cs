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
        private string username;
        private string? currentOpponent;
        private string? currentOpponentElo;


        public UserWindow(StreamReader reader, StreamWriter writer, string username)
        {
            this.reader = reader;
            this.writer = writer;
            this.username = username;
            InitializeComponent();
            hasiMezuakEntzuten();
            kargatuErabiltzailea();
        }

        private void hasiMezuakEntzuten()
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
                            lblIrabazita.Text = mezuarenzatiak[3];
                            lblGalduta.Text = mezuarenzatiak[4];
                            lblWinRate.Text = $"{mezuarenzatiak[5]}%";
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
                        MessageBox.Show($"Partidua hasi da {currentOpponent} aurka!");
                        // Ireki match window (etorkizunean)
                        // new MatchWindow(username, currentOpponent).Show();
                        // this.Close();
                    });
                    break;

                default:
                    Console.WriteLine($"DEBUG: Jasotako mezua: {mezua}");
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
            writer.WriteLine($"CHANGE_P:{username}:{txtNewPassword.Password}");
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
