namespace Nethostfire.MySQL{
    public class MySQLStateEventArgs : EventArgs {
        public MySQLState Status {get;}
        public MySQLStateEventArgs(MySQLState state){
            Status = state;
        }
    }
}