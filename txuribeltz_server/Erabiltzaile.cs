using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace txuribeltz_server
{
    internal class Erabiltzaile
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
