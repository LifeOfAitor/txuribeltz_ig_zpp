using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace txuribeltz
{
    public partial class LoginWindow : Window
    {
        private TcpClient client;
        private StreamWriter writer;
        private StreamReader reader;
        private NetworkStream ns;
        private Thread listenerThread;
        private bool shouldListen = true;
        private bool logeatuta = false;
        private bool konexioaEginda = false;
        private readonly Services.ValidationService validationService = new(); // testak egiteko

        public LoginWindow()
        {
            InitializeComponent();

            //zerbitzarira konektatuko gara lehenengo
            zerbitzariraKonektatu();
        }

        // zerbitzarira konektatzeko metodoa, errefaktorizatu beharko litzateke, beste klaseetan daukadan bezala
        // Adibidez hasiMezuakEntzuten() eta prozesatuMezuak() bezalako metodoak erabiliz
        public void zerbitzariraKonektatu()
        {
            try
            {
                client = new TcpClient();
                client.Connect(txtServerIp.Text, 13000); // Zerbitzariaren IP eta portua, defektuz localhost eta 13000 dira
                ns = client.GetStream();
                writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };
                reader = new StreamReader(ns, Encoding.UTF8);

                konexioaEginda = true;
                txt_erroreak.Text = "";
                txt_mezuak.Text += "Konektatuta zerbitzarira.\n";

                // Lehen konexioan zerbitzarira mezua bidali eta erantzuna jasotzeko.
                listenerThread = new Thread(() =>
                {
                    try
                    {
                        string line;
                        while (shouldListen && (line = reader.ReadLine()) != null)
                        {
                            Dispatcher.Invoke(() =>
                            {

                                // Zerbitzariaren aginduak kudeatu, zerbitzaritik datorren mezua AGINDUA:mezua formatua dakar

                                // Bakarrik loginarekin zerikusia daukaten aginduak kudeatuko dira hemen
                                if (line.StartsWith("LOGIN_OK"))
                                {
                                    logeatuta = true;
                                    string[] parts = line.Split(':');
                                    string userType = parts.Length > 1 ? parts[1] : "user";

                                    txt_mezuak.Text = "Login arrakastatsua!";

                                    // lehio honetatik ez entzun gehiago zerbitzariari
                                    shouldListen = false;

                                    // Ireki bakoitzaren lehioa
                                    if (userType == "admin")
                                    {
                                        AdminWindow adminWin = new AdminWindow(reader, writer);
                                        adminWin.Show();
                                    }
                                    else
                                    {
                                        UserWindow userWin = new UserWindow(reader, writer, txtUsuario.Text);
                                        userWin.Show();
                                    }

                                    this.Close();
                                }
                                else if (line.StartsWith("LOGIN_FAIL"))
                                {
                                    string[] parts = line.Split(':');
                                    txt_erroreak.Text = parts.Length > 1 ? parts[1] : "Login errorea";
                                    txt_mezuak.Text = "";
                                }
                                else if (line == "SIGNUP_OK")
                                {
                                    txt_mezuak.Text = "Erabiltzailea ondo sortu da!";
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        if (shouldListen)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                txt_erroreak.Text += "Bezeroa deskonektatua zerbitzaritik\n";
                                txt_mezuak.Text = "";
                                konexioaEginda = false;
                            });
                        }
                    }
                });
                listenerThread.IsBackground = true;
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Ezin izan da konektatu zerbitzarira: {ex.Message}");
                txt_erroreak.Text = "Ezin izan da konektatu zerbitzarira. Konexioa bilatzen...";
            }
        }

        //lehioa ixtean zerbitzaritik deskonektatuko da bezeroa baina bakarrik logeatu gabe dagoenean
        private void zerbitzaritikDeskonektatu()
        {
            //Errekurtsoak itxiko dira baina zerbitzariari deskonexioa egiteko esan, garbiagoa izateko
            try
            {
                if (!logeatuta)
                {
                    shouldListen = false;
                    if (client?.Connected == true)
                    {
                        writer.WriteLine("DISCONNECT");
                    }
                    reader?.Close();
                    writer?.Close();
                    ns?.Close();
                    if (client?.Connected == true)
                        client.Close();
                }

            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txt_mezuak.Text += "Errorea deskonektatzean: " + ex.Message + "\n";
                });
            }

        }
        /*
         * Sisteman logeatuko gara hemendik logeatzeko botoia sakatzen dugunean
         * Behar dugu:
         *  - erabiltzailea
         *  - pasahitza
         * Eta ez badago erabiltzailerik, sortzeko aukera sortu
        */
        public void login(object sender, RoutedEventArgs e)
        {
            try
            {
                var validation = validationService.ValidateLogin(txtUsuario.Text, txtPassword.Password);
                if (!validation.IsValid)
                {
                    txt_erroreak.Text = validation.ErrorMessage;
                    return;
                }

                // Zerbitzariari bidali log in egiteko mezua
                /*
                 *Zerbitzariak konprobnatuko du erabiltzaile mota eta horren arabera admin edo user bezala konektatuko da
                 */
                string message = $"LOGIN:{txtUsuario.Text}:{txtPassword.Password}";
                writer.WriteLine(message);
                txt_erroreak.Text = "";
                txt_mezuak.Text = "";
            }
            catch (Exception ex)
            {
                txt_erroreak.Text = $"Errorea login egitean: {ex.Message}, ez badaukazu, sortu erabiltzaile bat";
                txtUsuario.Clear();
                txtPassword.Clear();
            }
        }

        /*
         * Sisteman erabiltzaile berri bat sortuko dugu hemendik
         * Behar dugu:
         *  - erabiltzailea
         *  - pasahitza
        */
        private void newUser(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pass the writer to the signup window so it can communicate with the server
                Window signup = new SingUp(writer);
                signup.ShowDialog();
            }
            catch (Exception ex)
            {
                txt_erroreak.Text = $"Ezin izan da erabiltzailerik sortu: {ex.Message}";
            }
        }
        //enter sakatuz login egin ahal izateko
        //XAML fitxategian jarri PreviewKeyDown="Window_PreviewKeyDown"
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                login(this, new RoutedEventArgs());
            }
        }

        private void itxiAplikazioa(object sender, System.ComponentModel.CancelEventArgs e)
        {
            zerbitzaritikDeskonektatu();
        }

        // zerbitzarira konektatu botoia sakatuz, adibidez zerbitzarira aplikazioa irekitzerakoan ez badugu konexiorik lortzen, berriz probatu dezakegu botoiari sakatuz
        private void zerbitzariraKonektatu(object sender, RoutedEventArgs e)
        {
            if (!konexioaEginda)
            {
                zerbitzariraKonektatu();
            }
            else
            {
                txt_mezuak.Text = "Dagoeneko konektatuta zaude zerbitzarira.\n";
            }
        }
    }
}