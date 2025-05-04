namespace Nethostfire.MySQL{
    public class MySQLStateEventArgs : EventArgs {
        public MySQLState State {get;}
        public MySQLStateEventArgs(MySQLState state){
            State = state;
        }
    }
}