namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferUIMoveCondition
    {
        private string _moveToModule;
        public string MoveToModule
        {
            get { return _moveToModule; }
            set { _moveToModule = value; }
        }

        private int _moveToSlot;
        public int MoveToSlot
        {
            get { return _moveToSlot; }
            set { _moveToSlot = value; }
        }


    }
}
