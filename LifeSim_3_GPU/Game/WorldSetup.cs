using LifeSim_3_GPU.GPU_Side.KernelsThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.Game
{
    public struct WorldSize
    {
        public int W;
        public int H;

        public WorldSize(int w, int h)
        {
            W = w; H = h;
        }
    }
    public struct WorldSetup
    {
        public WorldSize Size;

        public ColorsForKernel Colors;

        public string Name { get; set; }

        public int ConditionIsActive_tr { get; set; }
        public int TurnEnergyCost { get; set; }
        public int GenLen { get; set; }
        public int CreatureLifeTime { get; set; }
        public int MaxActionPerTurn { get; set; }
        public float MutateChance { get; set; }
        public bool StrongMutateEnable { get; set; }
        public float StrongMutateChance { get; set; }
        public int HowMuchGenesMutateOnStrongMutate { get; set; }
        public float GensDifferenceForFamaly { get; set; }
        public int CorpsSpawnChance { get; set; }
        public int MinimumEnergyForMove { get; set; }
        public int MinimumEnergyForReprosuce { get; set; }
        public int ReproduceCost_Minerals { get; set; }
        public int ReproduceCost_Energy { get; set; }
        public int MinimumEnergyForAttack { get; set; }
        public int AttackCostEnergy { get; set; }
        public int TryAttackCostMinerals { get; set; }
        public bool EnableCounterAttack { get; set; }
        public int GiveMineralsValue { get; set; }
        public int GiveEnergyValue { get; set; }

        public int InitCreatureCount { get; set; }
        public int InitEnergy { get; set; }
        public int InitMinerals { get; set; }

        public bool CycleWorld_y { get; set; }
        public bool CycleWorld_x { get; set; }
        public bool SpawnWater { get; set; }
        public bool SpawnCorps { get; set; }
        public bool SpawnMinerals { get; set; }
        public double WaterLevel { get; set; }
        public int CollectMineralsFromWaterValue { get; set; }


        public WorldSetup(WorldSize size)
        {
            this = GetDeflout();
            Size = size;
        }

        private static WorldSetup GetDeflout()
        {
            ColorsForKernel c = new ColorsForKernel
            {
                BgColor = new(80, 80, 80),
                Game_BgColor = new(255, 255, 255), // Game_BgColor = new(255, 255, 255), // new(127, 127, 127)
                Game_Corpse_MainColor = new(115, 41, 41),
                Game_Mineral_DotColor = new(255, 255, 255),
                Game_Mineral_MainColor = new(125, 147, 224),
                Game_Water_BgColor = new(208, 230, 247)
            };

            return new WorldSetup
            {
                Name = "Deflout",
                Colors = c,

                TurnEnergyCost = 1,
                CollectMineralsFromWaterValue = 2,

                GenLen = 32,
                CreatureLifeTime = 300,
                MaxActionPerTurn = 1,
                MutateChance = 0.1f,
                StrongMutateEnable = true,
                StrongMutateChance = 0.001f,
                HowMuchGenesMutateOnStrongMutate = 5,
                GensDifferenceForFamaly = 0.2f,
                MinimumEnergyForMove = 50,
                MinimumEnergyForReprosuce = 50,
                ReproduceCost_Minerals = 5,
                ReproduceCost_Energy = 5,
                MinimumEnergyForAttack = 50,
                AttackCostEnergy = 20,
                TryAttackCostMinerals = 2,
                EnableCounterAttack = true,
                GiveMineralsValue = 10,
                GiveEnergyValue = 50,

                InitCreatureCount = 1_000_000, // 1_000_000_000
                InitEnergy = 300,
                InitMinerals = 100,

                SpawnWater = true,
                SpawnMinerals = true,
                SpawnCorps = true,
                CorpsSpawnChance = 5,

                CycleWorld_y = false,
                CycleWorld_x = false,
                WaterLevel = 1 - 0.33d,
            };
        }
    }
}
