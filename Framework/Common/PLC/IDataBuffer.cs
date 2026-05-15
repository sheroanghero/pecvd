//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Aitex.Core.RT.IOCore;

//namespace Aitex.Core.RT.PLC
//{
///// <summary>
///// The interfaces isn't common, depends on plc design
///// </summary>
///// <typeparam name="T"></typeparam>
///// <typeparam name="V"></typeparam>
//    public interface IDataBuffer<T, V>  where T : struct
//                                       where V : struct
//    {
//        T Input { get; set; }
//        V Output { get; set; }

//        void UpdateDI(bool[] values);
//        void UpdateAI(float[] values);

//        void UpdateDO(bool[] values);
//        void UpdateAO(float[] values);
//    }
//}
