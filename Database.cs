using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public class Database
    {

        //static void Main(string[] args)
        //{
        //    SQLiteConnection sqlite_conn;
        //    sqlite_conn = CreateConnection();
        //    CreateTable(sqlite_conn);
        //    InsertData(sqlite_conn);
        //    ReadData(sqlite_conn);
        //}

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
            string Createsql = "CREATE TABLE temperature (temperature VARCHAR(20), timestamp DATETIME)";
            string Createsql1 = "CREATE TABLE pressure (pressure VARCHAR(20), timestamp DATETIME)";
            string Createsql2 = "CREATE TABLE humidity (humidity VARCHAR(20), timestamp DATETIME)";
            string Createsql3 = "CREATE TABLE images (filePath VARCHAR(250), timestamp DATETIME)";
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = Createsql1;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = Createsql2;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = Createsql3;
            sqlite_cmd.ExecuteNonQuery();

        }

        public void InsertData(SQLiteConnection conn, String tableName, String colName, String value)
        {

            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO tableName (colName) VALUES (value); ";
           sqlite_cmd.ExecuteNonQuery();

        }

        static void ReadData(SQLiteConnection conn)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM atmospheric";

            sqlite_datareader = sqlite_cmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                string myreader = sqlite_datareader.GetString(0);
                Console.WriteLine(myreader);
            }
            conn.Close();
        }
    }
}