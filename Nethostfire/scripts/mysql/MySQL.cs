// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Data;
using System.Net;
using static Nethostfire.System;
namespace Nethostfire{
    public class MySQL {
        MySQLStatus CurrentMySQLStatus = MySQLStatus.Disconnected;
        dynamic? mySqlConnection;
        public MySQLStatus Status {get {return CurrentMySQLStatus;}}
        public Action<MySQLStatus>? OnStatus;

        /// <summary>
        /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
        /// </summary>
        public bool EnableLogs {get; set;} = true;

        public async void Connect(IPAddress server, int port, string username, string password, string database){
            try{
                CurrentMySQLStatus = MySQLStatus.Connecting;
                RunOnMainThread(() => OnStatus?.Invoke(CurrentMySQLStatus));
                WriteLog("Connecting in " + server + ":" + port, this, EnableLogs);
                mySqlConnection = AssemblyDynamic.Get("MySqlConnector", "MySqlConnector.MySqlConnection");
                //mySqlConnection = new MySqlConnector.MySqlClient.MySqlConnection();
                mySqlConnection!.ConnectionString = "server="+server+";port="+ port +";database="+database+";user="+username+";password="+password+";";
                await mySqlConnection.OpenAsync();
                CurrentMySQLStatus = MySQLStatus.Connected;
                RunOnMainThread(() => OnStatus?.Invoke(CurrentMySQLStatus));
            }catch(Exception ex){
                WriteLog(ex.Message, this, EnableLogs);
            }
        }

        public void Close(){
            if(mySqlConnection != null){
                mySqlConnection.Close();
                CurrentMySQLStatus = MySQLStatus.Disconnected;
                RunOnMainThread(() => OnStatus?.Invoke(CurrentMySQLStatus));
            }
        }

        //Execute query
        public bool ExecuteQuery(string sql, object[] parameters){
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
            for(int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@" + (i + 1).ToString(), parameters[i]);
            if(command.ExecuteNonQuery() > 0)
                return true;
            else
                return false;
        }

        //Check query exist
        public bool Query(string sql, object[] parameters){
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
            for(int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@" + (i + 1).ToString(), parameters[i]);
            var reader = command.ExecuteReader();
            bool result = reader.HasRows;
            reader.Close();
            reader.Dispose();
            return result;
        }

        //Result select
        public bool Query(string sql, object[] parameters, out DataRow[] dataRows){
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
            for(int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@" + (i + 1).ToString(), parameters[i]);
            var reader = command.ExecuteReader();
            DataTable dataTable = new DataTable();
            dataTable.Load(reader);
            reader.Close();
            reader.Dispose();

            dataRows = dataTable.AsEnumerable().ToArray();
            
            if(dataRows.Length == 0)
                return false;

            return true;
        }
    }
}