using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensorData;

namespace SQLite
{
    public class Database
    {

        public SQLiteConnection CreateConnection()
        {

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; New = True; Compress = True;");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }

        public void CreateTable(SQLiteConnection conn)
        {

            SQLiteCommand sqlite_cmd;
            string Createsql = "CREATE TABLE IF NOT EXISTS sensor (temperature VARCHAR(20), pressure VARCHAR(20), timestamp DATETIME)";
            string Createsql1 = "CREATE TABLE IF NOT EXISTS images (filePath VARCHAR(250), timestamp DATETIME)";
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = Createsql1;
            sqlite_cmd.ExecuteNonQuery();
        }

        public void InsertData(SQLiteConnection conn, String filePath, Sensor sensor)
        {
            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            SQLiteCommand sqlite_cmd;
            if (sensor == null)
            {
                sqlite_cmd = new SQLiteCommand("INSERT INTO images (filePath, timestamp) VALUES (?,?)", conn);
                sqlite_cmd.Parameters.Add(filePath);
                sqlite_cmd.Parameters.Add(Timestamp);
            }
            else
            {
                sqlite_cmd = new SQLiteCommand("INSERT INTO sensor (temperature, pressure, timestamp) VALUES (?,?,?)", conn);
                sqlite_cmd.Parameters.Add(sensor.temp);
                sqlite_cmd.Parameters.Add(sensor.pressure);
                sqlite_cmd.Parameters.Add(Timestamp);
            }

            try
            {
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
