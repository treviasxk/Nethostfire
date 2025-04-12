// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Data;
using System.Net;
using static Nethostfire.System;
namespace Nethostfire.MySQL{
    public class MySQL {
        dynamic? mySqlConnection;
        MySQLState state = MySQLState.Disconnected;
        public MySQLState State {get {return state;}}

        public event EventHandler<MySQLStateEventArgs>? StateChanged;

        /// <summary>
        /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
        /// </summary>
        public bool EnableLogs {get; set;} = true;

        public async void Connect(IPAddress server, int port, string username, string password, string database){
            try{
                ChangeState(MySQLState.Connecting, $"Connecting in {server}:{port}");
                mySqlConnection = AssemblyDynamic.Get("MySqlConnector", "MySqlConnection");
                //mySqlConnection = new MySqlConnector.MySqlClient.MySqlConnection();
                mySqlConnection!.ConnectionString = "server="+server+";port="+ port +";database="+database+";user="+username+";password="+password+";";
                await mySqlConnection.OpenAsync();
                ChangeState(MySQLState.Connected);
            }catch(Exception ex){
                WriteLog(ex.Message, this, EnableLogs);
            }
        }

        void ChangeState(MySQLState mySQLState, string message = ""){
            state = mySQLState;

            if(message == ""){
                WriteLog(mySQLState, this, EnableLogs);
            }else{
                WriteLog(message, this, EnableLogs);
            }

            RunOnMainThread(() => StateChanged?.Invoke(this, new MySQLStateEventArgs(mySQLState)));
        }

        public void Close(){
            if(mySqlConnection != null){
                mySqlConnection.Close();
                ChangeState(MySQLState.Disconnected);
            }
        }

        //Execute query
        public bool ExecuteQuery(string sql, object[] parameters){
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlCommand")(sql, mySqlConnection);
            for(int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@" + (i + 1).ToString(), parameters[i]);
            if(command.ExecuteNonQuery() > 0)
                return true;
            else
                return false;
        }

        //Check query exist
        public bool Query(string sql, object[] parameters){
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlCommand")(sql, mySqlConnection);
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
            dynamic command = AssemblyDynamic.Get("MySqlConnector", "MySqlCommand")(sql, mySqlConnection);
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