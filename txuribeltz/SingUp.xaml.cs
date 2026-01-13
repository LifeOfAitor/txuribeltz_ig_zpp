using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

        //enter sakatuz erabiltzailea sortu ahal izateko
        //XAML fitxategian jarri PreviewKeyDown="Window_PreviewKeyDown"
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnRegistratu_Click(this, new RoutedEventArgs());
            }
        }
    }
}
