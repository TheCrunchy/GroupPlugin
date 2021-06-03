using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace AlliancesPlugin
{
    class DatabaseForBank
    {
        static void Main(string[] args)
        {
            SQLiteConnection sqlite_conn;
            sqlite_conn = CreateConnection();
            CreateTable(sqlite_conn);
            InsertData(sqlite_conn);
            ReadData(sqlite_conn);
        }

        static SQLiteConnection CreateConnection()
        {

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=" + AlliancePlugin.path + "//BankDatabase.db; Version = 3; New = True; Compress = True; ");
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

        static void CreateTable(SQLiteConnection conn)
        {

            SQLiteCommand sqlite_cmd;
            string Createsql = "CREATE TABLE BankBalances(allianceId CHAR(36), balance BIGINT)";
           string Createsql1 = "CREATE TABLE BankRecords(allianceId CHAR(36), balanceNow BIGINT, balanceAfter BIGINT, change BIGINT, reason VARCHAR(50), steamId BIGINT, date DATETIME)";
           sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = Createsql1;
            sqlite_cmd.ExecuteNonQuery();

        }

        static void InsertData(SQLiteConnection conn)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
           // sqlite_cmd.CommandText = "INSERT INTO SampleTable
           //    (Col1, Col2) VALUES('Test Text ', 1); ";
           //sqlite_cmd.ExecuteNonQuery();
           // sqlite_cmd.CommandText = "INSERT INTO SampleTable
           //    (Col1, Col2) VALUES('Test1 Text1 ', 2); ";
           //sqlite_cmd.ExecuteNonQuery();
           // sqlite_cmd.CommandText = "INSERT INTO SampleTable
           //    (Col1, Col2) VALUES('Test2 Text2 ', 3); ";
           //sqlite_cmd.ExecuteNonQuery();


           // sqlite_cmd.CommandText = "INSERT INTO SampleTable1
           //    (Col1, Col2) VALUES('Test3 Text3 ', 3); ";
           sqlite_cmd.ExecuteNonQuery();

        }

        static void ReadData(SQLiteConnection conn)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM SampleTable";

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
