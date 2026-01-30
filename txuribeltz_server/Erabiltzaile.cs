namespace txuribeltz_server
{
    /*
     * Admin erabiltzaileak nahi dituen operazioak egiteko behar duen erabiltzaileen informazioa gordeko du objetu honek
     */
    public class Erabiltzaile
    {
        public string Erabiltzailea { get; set; }
        public string Mota { get; set; }
        public string Pasahitza { get; set; }

        public override string ToString()
        {
            return $"Erabiltzailea: {Erabiltzailea}\n" +
                    $"Mota: {Mota}\n" +
                    $"Pasahitza: {Pasahitza}";
        }

    }
}
