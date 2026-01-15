using System;
using System.Collections.Generic;
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
    /// Interaction logic for PasahitzaAldatu.xaml
    /// </summary>
    public partial class PasahitzaAldatu : Window
    {
        public string PasahitzaBerria { get; private set; }

        public PasahitzaAldatu()
        {
            InitializeComponent();
        }

        // pasahitza gorde botoia sakatuz
        private void pasahitzaAldatu(object sender, RoutedEventArgs e)
        {
            try
            {
                // Egiaztatu pasahitza ez dagoela hutsik
                if (string.IsNullOrWhiteSpace(txtbox_pasahitzberria.Text))
                {
                    lbl_mezua.Content = "Pasahitza ezin da hutsik egon.";
                    return;
                }

                // Gorde pasahitz berria eta itxi lehioa
                PasahitzaBerria = txtbox_pasahitzberria.Text;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                lbl_mezua.Content =$"Errorea pasahitza aldatzean: {ex.Message}";
            }
        }
    }
}
