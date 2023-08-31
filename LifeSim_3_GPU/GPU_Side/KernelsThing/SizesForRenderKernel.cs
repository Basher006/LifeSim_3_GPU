using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public struct SizesForRenderKernel
    {
        public int scale_i;
        public int set_scale;
        public int get_scale;

        public long sourse_len;

        public int sourse_w;
        public int sourse_h;

        public int res_w;
        public int res_h;

        public int pos_x;
        public int pos_y;
    }
}
