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
        private bool partidaAurkituta = false;
        private StreamWriter writer;
        private StreamReader reader;
        string username;


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
            // mezua komandoak izango dira adibidez:
            // LOGIN:erabiltzailea:pasahitza
            string[] mezuarenzatiak = mezua.Split(':');
            // agindua gordeko dugu eta horren arabera metodo bat edo bestea erabiliko dugu
            string agindua = mezuarenzatiak[0];

            switch (agindua)
            {
                case "DATA":
                    Dispatcher.Invoke(() =>
                    {
                        lblUsername.Text = mezuarenzatiak[1];
                        lblElo.Text = mezuarenzatiak[2];
                        lblIrabazita.Text = mezuarenzatiak[3];
                        lblGalduta.Text = mezuarenzatiak[4];
                        lblWinRate.Text = $"{mezuarenzatiak[5]}%";
                    }); 
                    break;
                default:
                    writer.WriteLine("ERROR:Agindu ezezaguna edo parametro falta");
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
            writer.WriteLine("FIND_MATCH");
            if (partidaAurkituta)
            {
                partidaInformazioa.Visibility = Visibility.Visible;
            }
        }

        private void StartMatch_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
