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
        private Button[,] taulakoBotoiak;
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

        // Zerbitzaritik jasotzen diren mezuak kudeatuko dira hemendik
        private void prozesatuMezua(string mezua)
        {
            // Mezuak formatu hau izango du: AGINDUA:informazioa:informazioa...
            // Zerbitzarian kudeatuko dira aginduak
            string[] mezuarenzatiak = mezua.Split(':');
            string agindua = mezuarenzatiak[0];

            switch (agindua)
            {
                case "ERROR":
                    MessageBox.Show(mezuarenzatiak[1]);
                    break;

                // Txataren barruan bidaltzen diren mezuak kudeatu
                case "CHAT":
                    // CHAT:bidaltzailea:mezua");
                    string bidaltzailea = mezuarenzatiak[1];
                    string txat_mezua = mezuarenzatiak[2];
                    bool isOwn = bidaltzailea.Equals(erabiltzailea, StringComparison.OrdinalIgnoreCase);
                    gehituTxatMezua(bidaltzailea, txat_mezua, isOwn);
                    break;

                // Jokalariek egiten dituzten mugimenduak kudeatu
                case "MOVE":
                    // Formatua: MOVE:jokalaria:row,col:pieza
                    string moveJokalaria = mezuarenzatiak[1];
                    string[] coords = mezuarenzatiak[2].Split(',');
                    int row = int.Parse(coords[0]);
                    int col = int.Parse(coords[1]);
                    string pieza = mezuarenzatiak[3];
                    // eguneratu taula erakusteko mugimentuak (aldaketak)
                    eguneratuTaula(row, col, pieza);
                    break;

                // jokalarien txandak kudeatzeko, txandaren arabera agertuko da textua menuan
                case "TURN":
                    string txandakoJokalaria = mezuarenzatiak[1];
                    bool nireTxanda = txandakoJokalaria.Equals(erabiltzailea, StringComparison.OrdinalIgnoreCase);
                    // eguneratu txtua bakoitzaren txandaren arabera
                    lblGameStatus.Text = nireTxanda ? "Zure txanda!" : $"{aurkalaria}-ren txanda";
                    lblGameStatus.Foreground = nireTxanda ? Brushes.LimeGreen : Brushes.Orange;
                    break;

                // partida bukatzerakoan egin beharko diren ekintzak kudeatu
                // mezuak irakurtzen bukatuko du eta erabiltzailearen menura bueltatuko da
                case "MATCH_END":
                    string emaitza = mezuarenzatiak.Length > 1 ? mezuarenzatiak[1] : "";
                    string endMezua = mezuarenzatiak.Length > 2 ? mezuarenzatiak[2] : "Partida amaitu da";
                    shouldListen = false;
                    gehituTxatMezua("SYSTEM", endMezua, false);
                    // partida bukatu dela adieraziko du
                    MessageBox.Show(endMezua);
                    // erabiltzaile menura bueltatuko gara
                    UserWindow userWin = new UserWindow(reader, writer, erabiltzailea);
                    userWin.Show();
                    this.Close();
                    break;
                default:
                    break;
            }
        }

        // modu dinamikoan jokuaren taula sortzeko metodoa
        private void tablaHasi()
        {
            taulakoBotoiak = new Button[15, 15];

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

                    // edozein botoi klikatzean Cell_Click metodoa deituko da
                    cell.Click += Cell_Click;
                    taulakoBotoiak[row, col] = cell;
                    gameBoard.Children.Add(cell);
                }
            }
        }

        // Taula eguneratzeko metodoa, pieza bat jartzen du emandako posizioan eta emandako kolorearekin
        private void eguneratuTaula(int row, int col, string pieza)
        {
            Button cell = taulakoBotoiak[row, col];
            Ellipse stone = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = pieza == "B" ? Brushes.Black : Brushes.White,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            cell.Content = stone;
            cell.IsEnabled = false; // Prevent clicking occupied cells
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            Button cell = (Button)sender;
            string position = cell.Tag.ToString();
            
            // Zerbitzariari bidali posizioa
            writer.WriteLine($"MOVE:{position}");
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

        // Txat mezua bidaltzeko metodoa, zerbitzariari bidaltzen dio eta txatan erakusten du
        private void bidaliMezua()
        {
            string mezua = txtChatMessage.Text.Trim();
            if (!string.IsNullOrEmpty(mezua))
            {
                // zerbitzariari bidali
                // CHAT:mezua formatua erabiliz
                // mezua nork bidali duen zerbitzariak kudeatuko du
                // CHAT:mezua
                writer.WriteLine($"CHAT:{mezua}");

                // Guk bidali dugun mezua erakutsi txatan (momentuz ez)
                //gehituTxatMezua(erabiltzailea, mezua, true);
                txtChatMessage.Clear();
            }
        }

        //Txat mezua gehituko da erabiltzailearen arabera ezkerrean edo eskuinean eta kolera ezberdinekin
        private void gehituTxatMezua(string sender, string message, bool isOwn)
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

        // Amore emateko aukera kudeatzen da, bezeroak nahi duenean klikatu dezake
        // Lehioa itxiko da eta user lehioa irekiko da
        private void Surrender_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ziur zaude amore eman nahi duzula?", 
                                         "Amore eman", 
                                         MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                writer.WriteLine($"WIN:{aurkalaria}");
            }
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ziur zaude partida utzi nahi duzula? Galdu egingo duzu.",
                                         "Partida utzi",
                                         MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                writer.WriteLine($"WIN:{aurkalaria}");
            }
        }
    }
}
