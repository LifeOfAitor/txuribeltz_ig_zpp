using System.IO;
using System.Windows;

namespace txuribeltz
{
    /// <summary>
    /// Interaction logic for SingUp.xaml
    /// </summary>
    public partial class SingUp : Window
    {
        private StreamWriter writer;

        public SingUp(StreamWriter writer)
        {
            InitializeComponent();
            this.writer = writer;
        }

        // erabiltzailea sortu botoia sakatuz
        private void btnRegistratu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // zerbitzarira konektatu eta erabiltzaile hau sortu
                if (string.IsNullOrWhiteSpace(txtErabiltzailea.Text) || string.IsNullOrWhiteSpace(txtPasahitza.Password))
                {
                    txt_erroreak.Text = "Erabiltzaile edo pasahitza hutsik daude.";
                    return;
                }
                if (txtPasahitza.Password != txtPasahitza2.Password)
                {
                    txt_erroreak.Text = "Pasahitzak ez datoz bat.";
                    return;
                }

                // bidali signup zerbitzarira
                string message = $"SIGNUP:{txtErabiltzailea.Text}:{txtPasahitza.Password}";
                writer.WriteLine(message);
                Close();
            }
            catch (Exception ex)
            {
                // errorea erakutsi
                txt_erroreak.Text = $"Errorea erregistratzean: {ex.Message}";
            }
        }

        // lehioa itxi atzera botoia sakatuz
        private void btnAtzera_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
