using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public struct ColorsForKernel
    {
        public MyColor BgColor;

        public MyColor Game_BgColor;
        public MyColor Game_Water_BgColor;

        public MyColor Game_Corpse_MainColor;

        public MyColor Game_Mineral_MainColor;
        public MyColor Game_Mineral_DotColor;
    }
}
