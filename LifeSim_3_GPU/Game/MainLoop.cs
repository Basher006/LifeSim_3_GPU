using LifeSim_3_GPU.GPU_Side;
using LifeSim_3_GPU.GPU_Side.KernelsThing;

namespace LifeSim_3_GPU.Game
{
    public static class MainLoop
    {
        public static GPU_Context gpu;

        public static int turnCounterl = 0;

        private static Thread mainloop;
        public static void Init(RECT rect, float scale)
        {
            WorldSetup setup = new WorldSetup(new WorldSize(940, 490)); // (160, 160) (940, 490) (1300, 750) (1800, 950) (3000, 3000)
            World world = new World(setup, 70_000);
            GameScene.Init(world);
            Console.WriteLine("init done!");

            gpu = new(GameScene.Cells, GameScene.World.Setup);
            gpu.Cells_Render(rect, scale);
            mainloop = new Thread(Run);
        }

        public static void startThred()
        {
            mainloop.IsBackground = true;
            mainloop.Start();
        }

        public static void Run()
        {
            while (true)
            {
                var constants = createConstantsForTurn();
                constants.currentTurn = turnCounterl;
                gpu.Turn(constants);
                turnCounterl++;
            }
        }

        public static TurnKernelConstants createConstantsForTurn()
        {
            TurnKernelConstants constants = new();

            constants.Size = GameScene.World.Setup.Size;

            constants.ConditionDiv = 10;
            constants.CommandDiv = 29;

            constants.GenLen = GameScene.World.Setup.GenLen;

            constants.currentTurn = 0;

            constants.MaxActionPerTurn = 10;
            constants.ConditionIsActive_tr = 127;

            constants.CreatureLifeTime = GameScene.World.Setup.CreatureLifeTime;
            constants.CollectMineralsFromWaterValue = 2;
            constants.MinimumEnergyForMove = GameScene.World.Setup.MinimumEnergyForMove;
            constants.MinimumEnergyForReprosuce = GameScene.World.Setup.MinimumEnergyForReprosuce;
            constants.ReproduceCost_Energy = 5;
            constants.ReproduceCost_Minerals = 5;
            constants.MineralsToEnergy_EnergyValue = 50;
            constants.MineralsToEnergy_MineralsValue = 5;
            constants.EnergyToMinerals_EnergyValue = 50;
            constants.EnergyToMinerals_MineralsValue = 5;

            constants.MutateChance = GameScene.World.Setup.MutateChance;
            constants.StrongMutateEnable = GameScene.World.Setup.StrongMutateEnable ? (byte)0 : (byte)0;
            constants.StrongMutateChance = GameScene.World.Setup.StrongMutateChance;
            constants.HowMuchGenesMutateOnStrongMutate = GameScene.World.Setup.HowMuchGenesMutateOnStrongMutate;
            constants.GensDifferenceForFamaly = GameScene.World.Setup.GensDifferenceForFamaly;

            constants.MinimumEnergyForAttack = GameScene.World.Setup.MinimumEnergyForAttack;
            constants.TryAttackCostMinerals = GameScene.World.Setup.TryAttackCostMinerals;
            constants.AttackCostEnergy = GameScene.World.Setup.AttackCostEnergy;
            constants.EnableCounterAttack = GameScene.World.Setup.EnableCounterAttack ? (byte)0 : (byte)0;

            constants.InitCreatureCount = GameScene.World.Setup.InitCreatureCount;
            constants.InitEnergy = GameScene.World.Setup.InitEnergy;
            constants.InitMinerals = GameScene.World.Setup.InitMinerals;
            constants.CycleWorld_x = GameScene.World.Setup.CycleWorld_x ? (byte)1 : (byte)0;
            constants.CycleWorld_y = GameScene.World.Setup.CycleWorld_y ? (byte)1 : (byte)0;

            return constants;
        }
    }
}
