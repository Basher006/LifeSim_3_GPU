using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.Game
{
    public class World
    {
        public WorldSetup Setup;

        public World(WorldSetup setup, int initCreatures)
        {
            Setup = setup;
        }
    }
}
