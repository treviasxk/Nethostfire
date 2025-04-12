// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Net;

namespace Nethostfire.UDP{
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
        public byte[] Data { get; }
        public int GroupID { get; }
        public ClientDataReceivedEventArgs(byte[] bytes, int groupID){
            Data = bytes;
            GroupID = groupID;
        }
    }

    public class ServerDataReceivedEventArgs : EventArgs{
        public byte[] Data { get; }
        public int GroupID { get; }
        public IPEndPoint IP { get; }
        public ServerDataReceivedEventArgs(byte[] bytes, int groupID, IPEndPoint ip){
            Data = bytes;
            GroupID = groupID;
            IP = ip;
        }
    }
}