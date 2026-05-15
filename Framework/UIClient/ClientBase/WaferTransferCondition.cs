using MECF.Framework.Common.CommonData;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferTransferCondition
    {
        private bool _IsPassAligner;
        public bool IsPassAligner
        {
            get { return _IsPassAligner; }
            set { _IsPassAligner = value; }
        }

        private int _AlignerAngle;
        public int AlignerAngle
        {
            get { return _AlignerAngle; }
            set { _AlignerAngle = value; }
        }

        private bool _IsPassCooling;
        public bool IsPassCooling
        {
            get { return _IsPassCooling; }
            set { _IsPassCooling = value; }
        }

        private int _CoolingTime;
        public int CoolingTime
        {
            get { return _CoolingTime; }
            set { _CoolingTime = value; }
        }

        private RobotArm _Blade;
        public RobotArm Blade
        {
            get { return _Blade; }
            set { _Blade = value; }
        }
    }
}
