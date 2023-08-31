using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{
    public struct FlagsForKernel
    {
        public bool this_is_cell;
        public bool top_is_cell;
        public bool left_is_cell;
        public bool leftTop_is_cell;

        public bool is_spawn;
        public bool topCell_is_spawned;
        public bool leftCell_is_spawned;
        public bool leftTopCell_is_spawned;

        public bool isNeedDecriseVal_on_left_side;
        public bool isNeedDecriseVal_on_top_side;
        public bool isNeedDecriseVal_on_leftTop_side;
    }
}
