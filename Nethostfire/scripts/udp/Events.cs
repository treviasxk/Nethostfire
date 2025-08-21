// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;
using System.Text;

namespace TreviasXk.UDP{
    public class SessionStatusEventArgs : EventArgs{
        public SessionStatus Status { get; }

        public SessionStatusEventArgs(SessionStatus status){
            Status = status;
        }
    }

    public class ServerStateEventArgs : EventArgs{
        public ServerState Status { get; }

        public ServerStateEventArgs(ServerState status){
            Status = status;
        }
    }

    public class SessionEventArgs : EventArgs{
        public IPEndPoint IP { get; }
        public Session Session {get;}
        public SessionEventArgs(IPEndPoint ip, Session session){
            IP = ip;
            Session = session;
        }
    }


    public class ClientDataReceivedEventArgs : EventArgs{
        internal Client Client;
        public byte[] Bytes { get; }
        public string BytesToString {get{ return Encoding.UTF8.GetString(Bytes); }}
        public int BytesToInt {get{ return BitConverter.ToInt32(Bytes); }}
        public int GroupID { get; }
        public int Ping {get{ return Client.Session.Ping; }}

        public ClientDataReceivedEventArgs(Client client, byte[] bytes, int groupID)
        {
            Client = client;
            Bytes = bytes;
            GroupID = groupID;
        }
    }

    public class ServerDataReceivedEventArgs : EventArgs{
        internal Server Server;
        public byte[] Bytes { get; }
        public string BytesToString {get{ return Encoding.UTF8.GetString(Bytes); }}
        public int BytesToInt {get{ return BitConverter.ToInt32(Bytes); }}
        public int GroupID { get; }
        public ushort Ping {get
            {
                if (Server.Sessions.TryGetValue(IP, out var session))
                    return Server.Sessions[IP].Ping;
                else
                    return 0;
            }
        }
        public IPEndPoint IP { get; }

        public ServerDataReceivedEventArgs(Server server, byte[] bytes, int groupID, IPEndPoint ip){
            Server = server;
            Bytes = bytes;
            GroupID = groupID;
            IP = ip;
        }
    }
}