using LifeSim_3_GPU.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU.GPU_Side.KernelsThing
{

    public struct TurnKernelConstants
    {
        public WorldSize Size;

        public int startIndex_x;
        public int startIndex_y;

        public int ConditionDiv;
        public int CommandDiv;

        public int GenLen;

        public long currentTurn;

        public int MaxActionPerTurn;
        public int ConditionIsActive_tr;

        public int MinimumEnergyForMove;
        public int MinimumEnergyForReprosuce;
        public float MutateChance;
        public byte StrongMutateEnable;
        public float StrongMutateChance;
        public int HowMuchGenesMutateOnStrongMutate;
        public float GensDifferenceForFamaly;

        public int MinimumEnergyForAttack;
        public int TryAttackCostMinerals;
        public int AttackCostEnergy;
        public byte EnableCounterAttack;

        public int CreatureLifeTime;
        public int CollectMineralsFromWaterValue;
        public int ReproduceCost_Energy;
        public int ReproduceCost_Minerals;
        public int MineralsToEnergy_EnergyValue;
        public int MineralsToEnergy_MineralsValue;
        public int EnergyToMinerals_EnergyValue;
        public int EnergyToMinerals_MineralsValue;

        public int InitCreatureCount;
        public int InitEnergy;
        public int InitMinerals;
        public byte CycleWorld_y;
        public byte CycleWorld_x;

        public const float CRATURE_VAL = 0.9f;
        public const float CREATURE_SAT = 0.85f;
    }
}
