using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace txuribeltz
{
    public partial class UserWindow : Window
    {
        private bool isInQueue = false;
        private string currentUsername = "";

        public UserWindow(string username = "")
        {
            InitializeComponent();
            currentUsername = username;
            LoadUserData();
            LoadTopPlayers();
        }

        /// <summary>
        /// Loads user data and displays it in the profile section
        /// </summary>
        private void LoadUserData()
        {
            // TODO: Load actual user data from database/API
            // For now, using sample data
            lblUsername.Text = currentUsername.Length > 0 ? currentUsername : "JokalariTestua";
            lblEmail.Text = "user@txuribeltz.eus";
            lblRank.Text = "Mailakoa 5";
            lblMatches.Text = "15";
            lblWinRate.Text = "60%";
        }

        /// <summary>
        /// Loads and displays the top 10 players
        /// </summary>
        private void LoadTopPlayers()
        {
            // TODO: Load actual top players from database/API
            topPlayersPanel.Children.Clear();

            // Sample data - replace with actual data
            var topPlayers = new[]
            {
                new { Rank = 1, Name = "XegoaLight", Rating = 2450 },
                new { Rank = 2, Name = "SuperJokalaria", Rating = 2380 },
                new { Rank = 3, Name = "ZubiProfesionala", Rating = 2290 },
                new { Rank = 4, Name = "Txuri Master", Rating = 2150 },
                new { Rank = 5, Name = "Beltz Ninja", Rating = 2080 },
                new { Rank = 6, Name = "Partida Txar", Rating = 1950 },
                new { Rank = 7, Name = "Ilea Ederrean", Rating = 1880 },
                new { Rank = 8, Name = "Jokaldi Txukun", Rating = 1750 },
                new { Rank = 9, Name = "Aurrera Beti", Rating = 1680 },
                new { Rank = 10, Name = "Azkena Mailakoaa", Rating = 1590 }
            };

            foreach (var player in topPlayers)
            {
                var playerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 35
                };

                // Rank number
                var rankTextBlock = new TextBlock
                {
                    Text = $"#{player.Rank}",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Width = 35,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Player name
                var nameTextBlock = new TextBlock
                {
                    Text = player.Name,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    FontSize = 12,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 120
                };

                // Rating
                var ratingTextBlock = new TextBlock
                {
                    Text = $"{player.Rating}",
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 168, 107)),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right,
                    Width = 60
                };

                playerPanel.Children.Add(rankTextBlock);
                playerPanel.Children.Add(nameTextBlock);
                playerPanel.Children.Add(ratingTextBlock);

                topPlayersPanel.Children.Add(playerPanel);
            }
        }

        /// <summary>
        /// Toggles between view mode and edit mode for the user profile
        /// </summary>
        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            userInfoDisplay.Visibility = Visibility.Collapsed;
            editFormPanel.Visibility = Visibility.Visible;
            btnEditProfile.IsEnabled = false;
        }

        /// <summary>
        /// Saves the changes made to the user profile
        /// </summary>
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            string newEmail = txtNewEmail.Text.Trim();
            string newPassword = txtNewPassword.Password.Trim();

            // Validation
            if (string.IsNullOrEmpty(newEmail) && string.IsNullOrEmpty(newPassword))
            {
                txtErrorMessage.Text = "Mesedez, aldatu beharreko datuak sartu.";
                return;
            }

            if (!string.IsNullOrEmpty(newEmail) && !newEmail.Contains("@"))
            {
                txtErrorMessage.Text = "Email baliogabea.";
                return;
            }

            // TODO: Save changes to database/API
            txtStatusMessage.Text = "Aldaketak gorde dira!";
            txtErrorMessage.Text = "";

            // Update display
            if (!string.IsNullOrEmpty(newEmail))
            {
                lblEmail.Text = newEmail;
            }

            // Reset form
            CancelEdit_Click(sender, e);
        }

        /// <summary>
        /// Cancels the profile editing and returns to view mode
        /// </summary>
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            editFormPanel.Visibility = Visibility.Collapsed;
            userInfoDisplay.Visibility = Visibility.Visible;
            btnEditProfile.IsEnabled = true;

            // Clear form
            txtNewEmail.Clear();
            txtNewPassword.Clear();
        }

        /// <summary>
        /// Adds the player to the match queue
        /// </summary>
        private void QueueMatch_Click(object sender, RoutedEventArgs e)
        {
            isInQueue = true;
            queueStatus.Text = "Ilerako zain...";
            queuePosition.Text = "Pozisio: #5";
            btnQueueMatch.Visibility = Visibility.Collapsed;
            btnCancelQueue.Visibility = Visibility.Visible;
            matchInfoBorder.Visibility = Visibility.Visible;
            lblOpponent.Text = "SuperJokalaria";
            lblWaitTime.Text = "00:35";
            txtStatusMessage.Text = "Ilerako gehitu zaude!";
        }

        /// <summary>
        /// Removes the player from the match queue
        /// </summary>
        private void CancelQueue_Click(object sender, RoutedEventArgs e)
        {
            isInQueue = false;
            queueStatus.Text = "Ez zaude ilerako";
            queuePosition.Text = "";
            btnQueueMatch.Visibility = Visibility.Visible;
            btnCancelQueue.Visibility = Visibility.Collapsed;
            matchInfoBorder.Visibility = Visibility.Collapsed;
            txtStatusMessage.Text = "Ilerako kenduta!";
        }

        /// <summary>
        /// Starts the match when both players are ready
        /// </summary>
        private void StartMatch_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open match window and navigate to game
            MessageBox.Show("Partida hasitakoa! (Ez da inplementatuta oraindik)", "Txuribeltz");
        }

        /// <summary>
        /// Logs out the user and closes the window
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Clear session data and navigate back to login
            MessageBoxResult result = MessageBox.Show(
                "Benetan saioa itxi nahi duzu?",
                "Txuribeltz - Saioa Itxi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: Call logout API/method
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
