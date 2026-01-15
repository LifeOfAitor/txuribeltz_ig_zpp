using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using txuribeltz_server;

namespace txuribeltz
{
    public partial class UserWindow : Window
    {
        private bool kolanDago = false;
        private bool partidaAurkituta = false;
        private StreamWriter writer;
        private StreamReader reader;
        List<Erabiltzaile> erabiltzaileak = new List<Erabiltzaile>();


        public UserWindow(StreamReader reader, StreamWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
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
                writer.WriteLine("GET_USER");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errorea erabiltzaileak eskatzean: {ex.Message}");
            }
        }

        private void prozesatuMezua(string mezua)
        {
            
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
