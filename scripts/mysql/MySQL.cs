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
        /// The DebugLog when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
        /// </summary>
        public bool DebugLog {get; set;} = true;


        public async void Connect(IPAddress server, int port, string username, string password, string database){
            try{
                CurrentMySQLStatus = MySQLStatus.Connecting;
                OnStatus?.Invoke(CurrentMySQLStatus);
                ShowLog("Connecting in " + server + ":" + port);
                mySqlConnection = System.GetDynamicAssembly("MySqlConnector", "MySqlConnector.MySqlConnection");
                //mySqlConnection = new MySqlConnector.MySqlClient.MySqlConnection();
                mySqlConnection!.ConnectionString = "server="+server+";port="+ port +";database="+database+";user="+username+";password="+password+";";
                await mySqlConnection.OpenAsync();
                CurrentMySQLStatus = MySQLStatus.Connected;
                OnStatus?.Invoke(CurrentMySQLStatus);
            }catch(Exception ex){
                ShowLog(ex.Message);
            }
        }

        public void Close(){
            if(mySqlConnection != null){
                mySqlConnection.Close();
                CurrentMySQLStatus = MySQLStatus.Disconnected;
                OnStatus?.Invoke(CurrentMySQLStatus);
            }
        }

        //Execute query
        public bool ExecuteQuery(string sql, object[] parameters){
            dynamic command = System.GetDynamicAssembly("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
            for(int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@" + (i + 1).ToString(), parameters[i]);
            if(command.ExecuteNonQuery() > 0)
                return true;
            else
                return false;
        }

        //Check query exist
        public bool Query(string sql, object[] parameters){
            dynamic command = System.GetDynamicAssembly("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
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
            dynamic command = System.GetDynamicAssembly("MySqlConnector", "MySqlConnector.MySqlCommand")(sql, mySqlConnection);
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


        /// <summary>
        /// Create a server log, if SaveLog is enabled, the message will be saved in the logs.
        /// </summary>
        public void ShowLog(string message){
            if(DebugLog)
                Log("[MYSQL] " + message, SaveLog);
        }
    }
}