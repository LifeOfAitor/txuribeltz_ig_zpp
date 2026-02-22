using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace txuribeltz_server
{
    public static class PasahitzaHashHelper
    {
        // BCrypt hash-a sortzen du hemen (salt-arekin batera)
        public static string HashPassword(string password)
        {
            // WorkFactor = 12 da gomendatuena, balantzea segurtasun/errendimendua artean
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        // Pasahitza egiaztatzen du gordetako hash-arekin alderatuz
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
