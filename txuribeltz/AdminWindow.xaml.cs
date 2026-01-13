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

namespace txuribeltz
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private StreamWriter writer;
        private StreamReader reader;

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
            if (mezua.StartsWith("USERS_LIST:"))
            {
                // Parse user list: USERS_LIST:user1,user2,user3
                string[] parts = mezua.Split(':');
                if (parts.Length > 1)
                {
                    string[] users = parts[1].Split(',');
                    // Assuming you have a DataGrid named dgErabiltzaileak
                    dgUsers.ItemsSource = users.Select(u => new { Erabiltzailea = u }).ToList();
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
    }
}
