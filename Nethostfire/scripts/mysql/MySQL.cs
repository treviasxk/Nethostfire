// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Data;
using System.Net;
using static Nethostfire.Nethostfire;
namespace Nethostfire.MySQL{
    public class MySQL {
        dynamic? mySqlConnection = null;
        MySQLState state = MySQLState.Disconnected;
        public MySQLState State {get {return state;}}

        /// <summary>
        /// If the connection fails and results in error (Code: 1042), MySQL will automatically reconnect.
        /// </summary>
        public bool AutoReconnect {get; set;} = true;
        
        /// <summary>
        /// Timeout in seconds for the reconnection to MySQL. (The default value is 5 seconds).
        /// </summary>
        public int ReconnectTimeout {get; set;} = 5; // Timeout in seconds
        
        /// <summary>
        /// In the StateChanged Event you can check the current state of mysql
        /// </summary>
        public event EventHandler<MySQLStateEventArgs>? StateChanged;

        /// <summary>
        /// The EnableLogs when declaring false, the logs in Console.Write and Debug.Log of Unity will no longer be displayed. (The default value is true).
        /// </summary>
        public bool EnableLogs {get; set;} = true;

        public void Connect(IPAddress host, int port, string username, string password, string database) => Connect(host.ToString(), port, username, password, database);

        public void Connect(string host, int port, string username, string password, string database){
            if(State != MySQLState.Connected){
                ChangeState(MySQLState.Connecting, $"Connecting in {host}:{port}");
                var t = new Thread(async ()=>{
                    while(State == MySQLState.Connecting){
                        string runtimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                        string versionNumber = System.Text.RegularExpressions.Regex.Match(runtimeVersion, @"\d+\.\d+\.\d+").Value;
                        if(new Version(versionNumber) < new Version("9.0.0")){
                            ChangeState(MySQLState.Disconnected, "MySQL Connector only works with .NET 9.0.0 or higher");
                            return;
                        }

                        mySqlConnection = AssemblyDynamic.Get("MySqlConnector", "MySqlConnection");
                        mySqlConnection!.ConnectionString = "server="+host+";port="+ port +";database="+database+";user="+username+";password="+password+";";
                        
                        try{
                            await mySqlConnection.OpenAsync();
                            ChangeState(MySQLState.Connected);
                        }catch(Exception ex){
                            // Captura MySqlException dinamicamente
                            if(ex.GetType().FullName == "MySqlConnector.MySqlException" && ex.GetType().GetProperty("Number")?.GetValue(ex) is int number){
                                if(AutoReconnect && number == 1042){ // Unable to connect to any of the specified MySQL hosts.
                                    WriteLog($"{ex.Message} (Code: {number}), retry in {ReconnectTimeout} seconds", this, EnableLogs);
                                    Thread.Sleep(ReconnectTimeout * 1000);
                                    continue;
                                }
                                ChangeState(MySQLState.Disconnected, $"{ex.Message} (Code: {number})");
                            }
                        }
                        Thread.Sleep(ReconnectTimeout * 1000);
                    }
                });
                t.IsBackground = true;
                t.Start();
            }else
                WriteLog("MySQL already connected", this, EnableLogs);
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