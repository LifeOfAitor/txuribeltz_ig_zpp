using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace txuribeltz_server
{
    static class databaseOperations
    {
        private static NpgsqlDataSource dataSource;
        /*
         * Datu basera konektatzeko behar da NpSql instalatzea, horretarako NuGet Package Manager erabili daiteke.
         * Komando hau erabiliz: NuGet\Install-Package Npgsql -Version 10.0.1
         * Web orria: https://www.nuget.org/packages/Npgsql/
         */
        public static async Task ConnectDatabaseAsync()
        {
            string user = "admin";
            string password = "admin";
            string database = "txuribeltz";
            var connectionString = $"Host=localhost:5433;Username={user};Password={password};Database={database}";

            try
            {
                //sortzen du datu basera iristeko beharrezkoa den dataSource-a (ate bat bezala)
                dataSource = NpgsqlDataSource.Create(connectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea datu basera konektatzerakoan: {ex.Message}");
            }
        }

        // erabiltzaileak existitzen badira egiaztatzeko eta admin diren ala ez
        public static async Task<bool> checkErabiltzaileak(string erabiltzaile, string pasahitza)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return false;
            }

            const string query = "SELECT COUNT(*) FROM erabiltzaileak WHERE TRIM(username) = @erabiltzaile AND TRIM(password) = @pasahitza;";

            try
            {
                await using var conn = await dataSource.OpenConnectionAsync(); // datu basera konektatzen da
                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@erabiltzaile", erabiltzaile);
                cmd.Parameters.AddWithValue("@pasahitza", pasahitza);

                var result = await cmd.ExecuteScalarAsync();
                long count = result != null ? Convert.ToInt64(result) : 0;

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea eabiltzailea bilatzen: {ex.Message}");
                return false;
            }
        }

        //administratzailea den ala ez konprobatzen du
        public static async Task<bool> checkAdmin(string erabiltzaile)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return false;
            }

            const string query = "SELECT mota FROM erabiltzaileak WHERE TRIM(username) = @erabiltzaile;";

            try
            {
                await using var conn = await dataSource.OpenConnectionAsync(); // datu basera konektatzen da
                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@erabiltzaile", erabiltzaile);

                var tipo = await cmd.ExecuteScalarAsync();

                return tipo?.ToString()?.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea egon da administratzailea konprobatzeko momentuan: {ex.Message}");
                return false;
            }
        }

        //erabiltzaile berri bat sortzen du datu basean
        public static void sortuErabiltzailea(string izena, string pasahitza)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return;
            }
            const string query = "INSERT INTO erabiltzaileak (username, password, mota) VALUES (@izena, @pasahitza, 'user');";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@izena", izena);
                cmd.Parameters.AddWithValue("@pasahitza", pasahitza);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea erabiltzailea sortzerakoan: {ex.Message}");
            }
        }

        //ezarritako izena duen erabiltzailea ezabatzen du datu basetik
        public static void ezabatuErabiltzailea(string izena)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return;
            }
            const string query = "DELETE FROM erabiltzaileak WHERE TRIM(username) = @izena;";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@izena", izena);
                cmd.ExecuteNonQuery();
                Console.WriteLine($"{izena} erabiltzailea ondo ezabatu da.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea erabiltzailea ezabatzekoan: {ex.Message}");
            }
        }

        //eskatutako erabiltzailea pasahitza aldatzen du
        public static void aldatuPasahitza(string izena, string pasahitzaBerria)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return;
            }
            const string query = "UPDATE erabiltzaileak SET password = @pasahitzaBerria WHERE TRIM(username) = @izena;";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@izena", izena);
                cmd.Parameters.AddWithValue("@pasahitzaBerria", pasahitzaBerria);
                cmd.ExecuteNonQuery();
                Console.WriteLine($"{izena} erabiltzailearen pasahitza ondo aldatu da.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea pasahitza aldatzerakoan: {ex.Message}");
            }
        }

        //datu baseko erabiltzaile normalak kargatzen ditu lista batean
        //Kargatzen ditu izena, mota eta pasahitza
        //Aldi berean, erabiltzaile bakoitza inprimatzen du badaezpada ere
        public static List<Erabiltzaile> kargatuErabiltzaileak()
        {
            List<Erabiltzaile> erabiltzaileak = new List<Erabiltzaile>();
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return erabiltzaileak;
            }
            const string query = "SELECT TRIM(username), mota, TRIM(password) FROM erabiltzaileak WHERE mota = 'user';";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Erabiltzaile erabiltzailea = new Erabiltzaile {
                        Erabiltzailea = reader.GetString(0),
                        Mota = reader.GetString(1),
                        Pasahitza = reader.GetString(2)
                    };
                    erabiltzaileak.Add(erabiltzailea);
                    Console.WriteLine(erabiltzailea.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea erabiltzaileak kargatzerakoan: {ex.Message}");
            }
            return erabiltzaileak;
        }

        //erabiltzailea menuan sartzen denean lortuko duen informazio guztia bidaliko da string luze batean ":" banatuta
        public static string? lortuBezeroInformazioaMenurako(string erabiltzailea)
        {
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return null;
            }

            const string query = """
                                SELECT 
                                    e.username,
                                    e.elo,
                                    COALESCE(SUM(CASE WHEN m.winner_id = e.id THEN 1 ELSE 0 END), 0) AS wins,
                                    COALESCE(SUM(CASE WHEN m.winner_id != e.id AND (m.player1_id = e.id OR m.player2_id = e.id) THEN 1 ELSE 0 END), 0) AS losses,
                                    CASE 
                                        WHEN COALESCE(SUM(CASE WHEN m.winner_id = e.id THEN 1 ELSE 0 END), 0) + COALESCE(SUM(CASE WHEN m.winner_id != e.id AND (m.player1_id = e.id OR m.player2_id = e.id) THEN 1 ELSE 0 END), 0) = 0 
                                        THEN 0
                                        ELSE COALESCE(SUM(CASE WHEN m.winner_id = e.id THEN 1 ELSE 0 END), 0)::float / 
                                             (COALESCE(SUM(CASE WHEN m.winner_id = e.id THEN 1 ELSE 0 END), 0) + COALESCE(SUM(CASE WHEN m.winner_id != e.id AND (m.player1_id = e.id OR m.player2_id = e.id) THEN 1 ELSE 0 END), 0))
                                    END AS winrate
                                FROM 
                                    erabiltzaileak e
                                LEFT JOIN 
                                    partidak m ON e.id = m.player1_id OR e.id = m.player2_id
                                WHERE 
                                    TRIM(e.username) = @username
                                GROUP BY 
                                    e.id;
                                """;
            try
            {
                using var conn = dataSource.OpenConnection();
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("username", erabiltzailea);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    // Ez dago erabiltzaile hori datu basean
                    return string.Empty;
                }

                // Read columns in the same order as SELECT
                string username = reader.GetString(0);
                int elo = reader.GetInt32(1);
                int irabaziak = reader.GetInt32(2);
                int galduak = reader.GetInt32(3);
                double winrate = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4);

                int winrateBorobildu = (int)Math.Round(winrate*100, MidpointRounding.AwayFromZero);

                string erantzuna = $"DATA:{username}:{elo}:{irabaziak}:{galduak}:{winrateBorobildu}%";
                Console.WriteLine($"""
                                    {erabiltzailea.ToUpper()} erabiltzailearen informazioa: 
                                        ELO:{elo}
                                        IRABAZIAK:{irabaziak}
                                        GALDUAK:{galduak}
                                        WINRATE:{winrateBorobildu}%
                                    """);

                return erantzuna;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea {erabiltzailea.ToUpper()} informazioa lortzen: {ex}");
                return null;
            }
        }

        //erabiltzaile baten eloa lortzeko
        public static string eloLortu(string erabiltzailea)
        {
            string elo = "0";
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return elo;
            }
            const string query = "SELECT elo FROM erabiltzaileak WHERE TRIM(username) = @erabiltzailea;";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@erabiltzailea", erabiltzailea);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    elo = elo = reader.GetInt32(0).ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea erabiltzaileak kargatzerakoan: {ex.Message}");
            }
            return elo;
        }

        public static List<string> lortuTOP10()
        {
            List<string> top10 = new List<string>();
            if (dataSource == null)
            {
                Console.WriteLine("Ez dago konexiorik sortuta");
                return top10;
            }
            const string query = "SELECT TRIM(username), elo FROM erabiltzaileak ORDER BY elo DESC LIMIT 10;";
            try
            {
                using var conn = dataSource.OpenConnection(); // datu basera konektatzen da
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string linea = $"{reader.GetString(0)}|{reader.GetInt32(1)}";
                    top10.Add(linea);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errorea TOP 10 kargatzerakoan: {ex.Message}");
            }
            return top10;
        }
    }
}
