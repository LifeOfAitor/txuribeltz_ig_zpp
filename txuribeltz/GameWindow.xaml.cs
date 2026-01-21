using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace txuribeltz
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private StreamWriter writer;
        private StreamReader reader;
        private string erabiltzailea;
        private string aurkalaria;
        private Button[,] boardButtons;
        private bool shouldListen = true;

        public GameWindow(StreamReader reader, StreamWriter writer, string erabiltzailea, string erabiltzaileaElo, string aurkalaria, string aurkalariaElo)
        {
            InitializeComponent();
            this.reader = reader;
            this.writer = writer;
            this.erabiltzailea = erabiltzailea;
            this.aurkalaria = aurkalaria;

            // Jokalarien informazioa ezarri pantaian

            lblYourUsername.Text = erabiltzailea;
            lblYourElo.Text = erabiltzaileaElo;
            lblOpponentUsername.Text = aurkalaria;
            lblOpponentElo.Text = aurkalariaElo;

            tablaHasi();
            hasiMezuakEntzuten();
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

        private void prozesatuMezua(string mezua)
        {
            // Mezuak formatu hau izango du: AGINDUA:informazioa:informazioa...
            // Zerbitzarian kudeatuko dira aginduak
            string[] mezuarenzatiak = mezua.Split(':');
            string agindua = mezuarenzatiak[0];

            switch (agindua)
            {
                default:
                    break;
            }
        }

        // modu dinamikoan jokuaren taula sortzeko metodoa
        private void tablaHasi()
        {
            boardButtons = new Button[15, 15];

            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    Button cell = new Button
                    {
                        Background = new SolidColorBrush(Color.FromRgb(205, 133, 63)),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Tag = $"{row},{col}",
                        Cursor = Cursors.Hand
                    };

                    cell.Click += Cell_Click;
                    boardButtons[row, col] = cell;
                    gameBoard.Children.Add(cell);
                }
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            Button cell = (Button)sender;
            string position = cell.Tag.ToString();
            
            // Zerbitzariari bidali posizioa
            // writer.WriteLine($"MOVE:{position}");
            
            MessageBox.Show($"Klikatu duzu: {position}");
        }

        private void SendChat_Click(object sender, RoutedEventArgs e)
        {
            bidaliMezua();
        }

        private void ChatMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bidaliMezua();
            }
        }

        private void bidaliMezua()
        {
            string message = erabiltzailea.ToUpper() + "-> " + txtChatMessage.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                // zerbitzariari bidali
                writer.WriteLine($"CHAT:{message}");

                // Mezua erakutsi txatan (bakarrik gurea)
                AddChatMessage(erabiltzailea, message, true);
                txtChatMessage.Clear();
            }
        }

        private void AddChatMessage(string sender, string message, bool isOwn)
        {
            Border messageBorder = new Border
            {
                Background = isOwn ? new SolidColorBrush(Color.FromRgb(78, 204, 163)) : new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = isOwn ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 220
            };

            StackPanel messagePanel = new StackPanel();
            
            TextBlock senderText = new TextBlock
            {
                Text = sender,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };

            messagePanel.Children.Add(senderText);
            messagePanel.Children.Add(messageText);
            messageBorder.Child = messagePanel;
            
            chatMessages.Children.Add(messageBorder);
            chatScroll.ScrollToBottom();
        }

        private void Surrender_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ziur zaude amore eman nahi duzula?", 
                                         "Amore eman", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                writer.WriteLine($"WIN:{aurkalaria}");
                this.Close();
                UserWindow userWin = new UserWindow(reader, writer, erabiltzailea);
                userWin.Show();
            }
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ziur zaude partida utzi nahi duzula? Galdu egingo duzu.", 
                                         "Partida utzi", 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                writer.WriteLine($"WIN:{aurkalaria}");
                this.Close();
                UserWindow userWin = new UserWindow(reader, writer, erabiltzailea);
                userWin.Show();
            }
        }
    }
}
