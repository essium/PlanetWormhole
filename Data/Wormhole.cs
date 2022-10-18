using System;
using static PlanetWormhole.Constants;

namespace PlanetWormhole.Data
{
    internal class Wormhole
    {
        private static int INC_SPRAY_TIMES;
        private static int INC_ABILITY;
        private static int EXTRA_INC_SPRAY_TIMES;
        private int[] producedQuota;
        private int[] servedQuota;
        private int[] produced;
        private int[] served;
        private int[] buffer;
        private int inc;
        public int consumedProliferator;
        private int sumSpray;
        private bool spray;
        private PlanetFactory tmpFactory;

        public Wormhole()
        {
            producedQuota = new int[MAX_ITEM_COUNT];
            servedQuota = new int[MAX_ITEM_COUNT];
            produced = new int[MAX_ITEM_COUNT];
            served = new int[MAX_ITEM_COUNT];
            buffer = new int[MAX_ITEM_COUNT];
            Array.Clear(buffer, 0, MAX_ITEM_COUNT);
            inc = 0;
        }
        public static void PatchPlanet(Object obj)
        {
            PlanetThreadObject pto = (PlanetThreadObject)obj;
            Wormhole wormhole = pto.wormhole;
            PlanetFactory factory = pto.factory;
            wormhole.tmpFactory = factory;
            wormhole.Reset();
            wormhole.RegisterTrash();
            wormhole.RegisterPowerSystem();
            wormhole.RegisterMiner();
            wormhole.RegisterAssembler();
            wormhole.RegisterFractionator();
            wormhole.RegisterLab();
            wormhole.RegisterEjector();
            wormhole.RegisterSilo();
            wormhole.RegisterStorage();
            wormhole.RegisterStation();
            wormhole.Spray();
            wormhole.ConsumeBuffer();
            wormhole.ConsumeTrash();
            wormhole.ConsumeStorage();
            wormhole.ConsumePowerSystem();
            wormhole.ConsumeMiner();
            wormhole.ConsumeAssembler();
            wormhole.ConsumeFractionator();
            wormhole.ConsumeLab();
            wormhole.ConsumeEjector();
            wormhole.ConsumeSilo();
            wormhole.ConsumeStation();
        }

        public void PatchGlobal(GameData gameData)
        {
            Reset();
            for (int i = 0; i < gameData.factoryCount; i++)
            {
                tmpFactory = gameData.factories[i];
                RegisterTrash();
                RegisterStorage();
            }
        }

        private void Reset()
        {
            Array.Clear(producedQuota, 0, MAX_ITEM_COUNT);
            Array.Clear(servedQuota, 0, MAX_ITEM_COUNT);
            Array.Clear(produced, 0, MAX_ITEM_COUNT);
            Array.Clear(served, 0, MAX_ITEM_COUNT);
            spray = true;
            sumSpray = 0;
            consumedProliferator = 0;
        }

        private void RegisterAssembler()
        {
            AssemblerComponent[] pool = tmpFactory.factorySystem.assemblerPool;
            for (int i = 1; i < tmpFactory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            producedQuota[pool[i].products[j]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(3 * pool[i].requireCounts[j] - pool[i].served[j]);
                            sumSpray += count;
                            servedQuota[pool[i].needs[j]] += count;
                        }
                    }
                }
            }
        }

        private void ConsumeAssembler()
        {
            AssemblerComponent[] pool = tmpFactory.factorySystem.assemblerPool;
            for (int i = 1; i < tmpFactory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            int itemId = pool[i].products[j];
                            if (servedQuota[itemId] > produced[itemId])
                            {
                                int count = Math.Min(pool[i].produced[j], servedQuota[itemId] - produced[itemId]);
                                produced[itemId] += count;
                                pool[i].produced[j] -= count;
                            }
                        }
                    }
                    if (pool[i].produced.Length > 1)
                    {
                        int len = pool[i].produced.Length;
                        switch (pool[i].recipeType)
                        {
                            case ERecipeType.Smelt:
                                for (int j = 0; j < len; j++)
                                {
                                    if (pool[i].produced[j] + pool[i].productCounts[j] > 100
                                        && buffer[pool[i].products[j]] < BUFFER_SIZE)
                                    {
                                        pool[i].produced[j] -= pool[i].productCounts[j];
                                        buffer[pool[i].products[j]] += pool[i].productCounts[j];
                                    }
                                }
                                break;
                            case ERecipeType.Assemble:
                                for (int j = 0; j < len; j++)
                                {
                                    if (pool[i].produced[j] > pool[i].productCounts[j] * 9
                                        && buffer[pool[i].products[j]] < BUFFER_SIZE)
                                    {
                                        pool[i].produced[j] -= pool[i].productCounts[j];
                                        buffer[pool[i].products[j]] += pool[i].productCounts[j];
                                    }
                                }
                                break;
                            default:
                                for (int j = 0; j < len; j++)
                                {
                                    if (pool[i].produced[j] > pool[i].productCounts[j] * 19
                                        && buffer[pool[i].products[j]] < BUFFER_SIZE)
                                    {
                                        pool[i].produced[j] -= pool[i].productCounts[j];
                                        buffer[pool[i].products[j]] += pool[i].productCounts[j];
                                    }
                                }
                                break;
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemId = pool[i].needs[j];
                            if (producedQuota[itemId] > served[itemId])
                            {
                                int count = _positive(Math.Min(3 * pool[i].requireCounts[j] - pool[i].served[j], producedQuota[itemId] - served[itemId]));
                                served[itemId] += count;
                                pool[i].served[j] += count;
                                if (spray)
                                {
                                    inc -= count * INC_ABILITY;
                                    pool[i].incServed[j] += INC_ABILITY * count;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterStation()
        {
            StationComponent[] pool = tmpFactory.transport.stationPool;
            for (int i = 1; i < tmpFactory.transport.stationCursor; i++)
            {
                if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                {
                    StationStore[] storage = pool[i].storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                producedQuota[storage[j].itemId] += storage[j].count;
                            }
                            else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                int count = _positive(storage[j].max - storage[j].count);
                                servedQuota[storage[j].itemId] += count;
                            }
                        }
                    }
                    if (pool[i].needs[5] == Constants.WARPER && tmpFactory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        servedQuota[WARPER] += _positive(pool[i].warperMaxCount - pool[i].warperCount);
                    }
                }
            }
        }

        private void ConsumeStation()
        {
            StationComponent[] pool = tmpFactory.transport.stationPool;
            for (int i = 1; i < tmpFactory.transport.stationCursor; i++)
            {
                if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                {
                    if (pool[i].needs[5] == Constants.WARPER && tmpFactory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        if (producedQuota[WARPER] > served[WARPER])
                        {
                            int count = _positive(Math.Min(pool[i].warperMaxCount - pool[i].warperCount, producedQuota[WARPER] - served[WARPER]));
                            served[WARPER] += count;
                            pool[i].warperCount += count;
                        }
                    }
                }
            }
            for (int i = 1; i < tmpFactory.transport.stationCursor; i++)
            {
                if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                {
                    StationStore[] storage = pool[i].storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            int itemId = storage[j].itemId;
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                if (servedQuota[itemId] > produced[itemId])
                                {
                                    int count = Math.Min(storage[j].count, servedQuota[itemId] - produced[itemId]);
                                    produced[itemId] += count;
                                    storage[j].count -= count;
                                    int incAdd = _split_inc(storage[j].inc, count);
                                    storage[j].inc -= incAdd;
                                    inc += incAdd;
                                }
                            } else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                if (producedQuota[itemId] > served[itemId])
                                {
                                    int count = _positive(Math.Min(producedQuota[itemId] - served[itemId], storage[j].max - storage[j].count));
                                    served[itemId] += count;
                                    storage[j].count += count;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterPowerSystem()
        {
            PowerGeneratorComponent[] pool = tmpFactory.powerSystem.genPool;
            for (int i = 1; i < tmpFactory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && tmpFactory.gameData.history.TechUnlocked(IONOSPHERIC_TECH))
                    {
                        int count = _positive((72000 - pool[i].catalystPoint) / 3600);
                        sumSpray += count;
                        servedQuota[pool[i].catalystId] += count;
                        if (pool[i].productId > 0)
                        {
                            producedQuota[pool[i].productId] += (int) pool[i].productCount;
                        }
                    }
                    int[] fuelNeeds = ItemProto.fuelNeeds[pool[i].fuelMask];
                    if (pool[i].fuelId > 0 || (fuelNeeds != null && fuelNeeds.Length > 0))
                    {
                        int itemId;
                        if (pool[i].fuelId > 0)
                        {
                            itemId = pool[i].fuelId;
                        }
                        else if (fuelNeeds.Length > 0)
                        {
                            itemId = fuelNeeds[0];
                            pool[i].SetNewFuel(itemId, 0, 0);
                        }
                        else
                        {
                            continue;
                        }
                        int count = _positive(10 - pool[i].fuelCount);
                        sumSpray += count;
                        servedQuota[itemId] += count;
                    }
                }
            }
            PowerExchangerComponent[] excPool = tmpFactory.powerSystem.excPool;
            for (int i = 1; i < tmpFactory.powerSystem.excCursor; i++)
            {
                if (excPool[i].id == i && excPool[i].fullId > 0 && excPool[i].emptyId > 0)
                {
                    if (_float_equal(excPool[i].targetState, 1.0f))
                    {
                        producedQuota[excPool[i].fullId] += excPool[i].fullCount;
                        servedQuota[excPool[i].emptyId] += _positive(PowerExchangerComponent.maxCount - excPool[i].emptyCount);
                    } else if (_float_equal(excPool[i].targetState, -1.0f))
                    {
                        producedQuota[excPool[i].fullId] += excPool[i].emptyCount;
                        servedQuota[excPool[i].emptyId] += _positive(PowerExchangerComponent.maxCount - excPool[i].fullCount);
                    }
                }
            }
        }

        private void ConsumePowerSystem()
        {
            PowerGeneratorComponent[] pool = tmpFactory.powerSystem.genPool;
            for (int i = 1; i < tmpFactory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && tmpFactory.gameData.history.TechUnlocked(Constants.IONOSPHERIC_TECH))
                    {
                        int itemId = pool[i].catalystId;
                        if (producedQuota[itemId] > served[itemId])
                        {
                            int count = _positive(Math.Min((72000 - pool[i].catalystPoint) / 3600, producedQuota[itemId] - served[itemId]));
                            served[itemId] += count;
                            pool[i].catalystPoint += count * 3600;
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                pool[i].catalystIncPoint += count * 3600 * INC_ABILITY;
                            }
                        }
                        if (pool[i].productId > 0)
                        {
                            itemId = pool[i].productId;
                            if (servedQuota[itemId] > produced[itemId])
                            {
                                int count = Math.Min((int)pool[i].productCount, servedQuota[itemId] - produced[itemId]);
                                produced[itemId] += count;
                                pool[i].productCount -= count;
                            }
                        }
                    }
                    if (pool[i].fuelId > 0)
                    {
                        int itemId = pool[i].fuelId;
                        if (producedQuota[itemId] > served[itemId])
                        {
                            int count = _positive(Math.Min(10 - pool[i].fuelCount, producedQuota[itemId] - served[itemId]));
                            served[itemId] += count;
                            pool[i].fuelCount += (short) count;
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                pool[i].fuelInc += (short) (INC_ABILITY * count);
                            }
                        }
                    }
                }
            }
            PowerExchangerComponent[] excPool = tmpFactory.powerSystem.excPool;
            for (int i = 1; i < tmpFactory.powerSystem.excCursor; i++)
            {
                if (excPool[i].id == i && excPool[i].fullId > 0 && excPool[i].emptyId > 0)
                {
                    int fullIndex = excPool[i].fullId;
                    int emptyIndex = excPool[i].emptyId;
                    if (_float_equal(excPool[i].targetState, 1.0f))
                    {
                        if (servedQuota[fullIndex] > produced[fullIndex])
                        {
                            int count = Math.Min(excPool[i].fullCount, servedQuota[fullIndex] - produced[fullIndex]);
                            produced[fullIndex] += count;
                            excPool[i].fullCount -= (short) count;
                        }
                        if (producedQuota[emptyIndex] > served[emptyIndex])
                        {
                            int count = _positive(Math.Min(PowerExchangerComponent.maxCount - excPool[i].emptyCount, producedQuota[emptyIndex] - served[emptyIndex]));
                            served[emptyIndex] += count;
                            excPool[i].emptyCount += (short) count;
                        }
                    }
                    else if (_float_equal(excPool[i].targetState, -1.0f))
                    {
                        if (servedQuota[emptyIndex] > produced[emptyIndex])
                        {
                            int count = Math.Min(excPool[i].emptyCount, servedQuota[emptyIndex] - produced[emptyIndex]);
                            produced[emptyIndex] += count;
                            excPool[i].emptyCount -= (short)count;
                        }
                        if (producedQuota[fullIndex] > served[fullIndex])
                        {
                            int count = _positive(Math.Min(PowerExchangerComponent.maxCount - excPool[i].fullCount, producedQuota[fullIndex] - served[fullIndex]));
                            served[fullIndex] += count;
                            excPool[i].fullCount += (short)count;
                        }
                    }
                }
            }
        }

        private void RegisterMiner()
        {
            MinerComponent[] pool = tmpFactory.factorySystem.minerPool;
            for (int i = 1; i < tmpFactory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        producedQuota[pool[i].productId] += pool[i].productCount;
                    }
                }
            }
        }

        private void ConsumeMiner()
        {
            MinerComponent[] pool = tmpFactory.factorySystem.minerPool;
            for (int i = 1; i < tmpFactory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        int itemId = pool[i].productId;
                        if (servedQuota[itemId] > produced[itemId])
                        {
                            int count = Math.Min(pool[i].productCount, servedQuota[itemId] - produced[itemId]);
                            produced[itemId] += count;
                            pool[i].productCount -= count;
                        }
                    }
                }
            }
        }

        private void RegisterLab()
        {
            LabComponent[] pool = tmpFactory.factorySystem.labPool;
            for (int i = 1; i < tmpFactory.factorySystem.labCursor; i++)
            {
                if (pool[i].id == i && !pool[i].researchMode && pool[i].recipeId > 0)
                {
                    if (pool[i].productCounts != null && pool[i].productCounts.Length > 0)
                    {
                        for (int j = 0; j < pool[i].productCounts.Length; j++)
                        {
                            producedQuota[pool[i].products[j]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(4 - pool[i].served[j]);
                            sumSpray += count;
                            servedQuota[pool[i].needs[j]] += count;
                        }
                    }
                }
                if (pool[i].id == i && pool[i].researchMode)
                {
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive((36000 - pool[i].matrixServed[j]) / 3600);
                            sumSpray += count;
                            servedQuota[pool[i].needs[j]] += count;
                        }
                    }
                }
            }
        }
        

        private void ConsumeLab()
        {
            LabComponent[] pool = tmpFactory.factorySystem.labPool;
            for (int i = 1; i < tmpFactory.factorySystem.labCursor; i++)
            {
                if (pool[i].id == i && !pool[i].researchMode && pool[i].recipeId > 0)
                {
                    if (pool[i].productCounts != null && pool[i].productCounts.Length > 0)
                    {
                        for (int j = 0; j < pool[i].productCounts.Length; j++)
                        {
                            int itemId = pool[i].products[j];
                            if (servedQuota[itemId] > produced[itemId])
                            {
                                int count = Math.Min(pool[i].produced[j], servedQuota[itemId] - produced[itemId]);
                                produced[itemId] += count;
                                pool[i].produced[j] -= count;
                            }
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemId = pool[i].needs[j];
                            if (producedQuota[itemId] > served[itemId])
                            {
                                int count = _positive(Math.Min(4 - pool[i].served[j], producedQuota[itemId] - served[itemId]));
                                served[itemId] += count;
                                pool[i].served[j] += count;
                                if (spray)
                                {
                                    inc -= count * INC_ABILITY;
                                    pool[i].incServed[j] += INC_ABILITY * count;
                                }
                            }
                        }
                    }
                }
                if (pool[i].id == i && pool[i].researchMode)
                {
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemId = pool[i].needs[j];
                            if (producedQuota[itemId] > served[itemId])
                            {
                                int count = _positive(Math.Min((36000 - pool[i].matrixServed[j]) / 3600, producedQuota[itemId] - served[itemId]));
                                served[itemId] += count;
                                pool[i].matrixServed[j] += count * 3600;
                                if (spray)
                                {
                                    inc -= count * INC_ABILITY ;
                                    pool[i].matrixIncServed[j] += INC_ABILITY * count * 3600;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterEjector()
        {
            EjectorComponent[] pool = tmpFactory.factorySystem.ejectorPool;
            for (int i = 1; i < tmpFactory.factorySystem.ejectorCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    servedQuota[pool[i].bulletId] += count;
                }
            }
        }

        private void ConsumeEjector()
        {
            EjectorComponent[] pool = tmpFactory.factorySystem.ejectorPool;
            for (int i = 1; i < tmpFactory.factorySystem.ejectorCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemId = pool[i].bulletId;
                    if (producedQuota[itemId] > served[itemId])
                    {
                        int count = _positive(Math.Min(20 - pool[i].bulletCount, producedQuota[itemId] - served[itemId]));
                        served[itemId] += count;
                        pool[i].bulletCount += count;
                        if (spray)
                        {
                            inc -= count * INC_ABILITY;
                            pool[i].bulletInc += INC_ABILITY * count;
                        }
                    }
                }
            }
        }

        private void RegisterSilo()
        {
            SiloComponent[] pool = tmpFactory.factorySystem.siloPool;
            for (int i = 1; i < tmpFactory.factorySystem.siloCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    servedQuota[pool[i].bulletId] += count;
                }
            }
        }

        private void ConsumeSilo()
        {
            SiloComponent[] pool = tmpFactory.factorySystem.siloPool;
            for (int i = 1; i < tmpFactory.factorySystem.siloCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemId = pool[i].bulletId;
                    if (producedQuota[itemId] > served[itemId])
                    {
                        int count = _positive(Math.Min(20 - pool[i].bulletCount, producedQuota[itemId] - served[itemId]));
                        served[itemId] += count;
                        pool[i].bulletCount += count;
                        if (spray)
                        {
                            inc -= count * INC_ABILITY;
                            pool[i].bulletInc += INC_ABILITY * count;
                        }
                    }
                }
            }
        }

        private void RegisterStorage()
        {
            StorageComponent[] storagePool = tmpFactory.factoryStorage.storagePool;
            for (int i = 1; i < tmpFactory.factoryStorage.storageCursor; i++)
            {
                if (storagePool[i] != null && storagePool[i].id == i)
                {
                    if (storagePool[i].grids == null)
                    {
                        continue;
                    }
                    for(int j = 0; j < storagePool[i].grids.Length; j++)
                    {
                        if (storagePool[i].grids[j].itemId > 0)
                        {
                            producedQuota[storagePool[i].grids[j].itemId] += storagePool[i].grids[j].count;
                        }
                    }
                }
            }
            TankComponent[] tankPool = tmpFactory.factoryStorage.tankPool;
            for (int i = 1; i < tmpFactory.factoryStorage.tankCursor; i++)
            {
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        producedQuota[tankPool[i].fluidId] += tankPool[i].fluidCount;
                    }
                }
            }
        }

        private void ConsumeStorage()
        {
            StorageComponent[] storagePool = tmpFactory.factoryStorage.storagePool;
            for (int i = 1; i < tmpFactory.factoryStorage.storageCursor; i++)
            {
                if (storagePool[i] != null && storagePool[i].id == i)
                {
                    if (storagePool[i].grids == null)
                    {
                        continue;
                    }
                    bool change = false;
                    for (int j = 0; j < storagePool[i].grids.Length; j++)
                    {
                        if (storagePool[i].grids[j].itemId > 0)
                        {
                            int itemId = storagePool[i].grids[j].itemId;
                            if (servedQuota[itemId] > produced[itemId])
                            {
                                int count = Math.Min(storagePool[i].grids[j].count, servedQuota[itemId] - produced[itemId]);
                                produced[itemId] += count;
                                storagePool[i].grids[j].count -= count;
                                if (storagePool[i].grids[j].count <= 0)
                                {
                                    storagePool[i].grids[j].itemId = 0;
                                    storagePool[i].grids[j].count = 0;
                                    storagePool[i].grids[j].inc = 0;
                                }
                                int incAdd = _split_inc(storagePool[i].grids[j].inc, count);
                                inc += incAdd;
                                storagePool[i].grids[j].inc -= incAdd;
                                if (count > 0)
                                {
                                    change = true;
                                }
                            }
                        }
                    }
                    if (change)
                    {
                        storagePool[i].NotifyStorageChange();
                    }
                }
            }
            TankComponent[] tankPool = tmpFactory.factoryStorage.tankPool;
            for (int i = 1; i < tmpFactory.factoryStorage.tankCursor; i++)
            {
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        int itemId = tankPool[i].fluidId;
                        if (servedQuota[itemId] > produced[itemId])
                        {
                            int count = Math.Min(tankPool[i].fluidCount, servedQuota[itemId] - produced[itemId]);
                            produced[itemId] += count;
                            tankPool[i].fluidCount -= count;
                            if (tankPool[i].fluidCount <= 0)
                            {
                                tankPool[i].fluidId = 0;
                                tankPool[i].fluidCount = 0;
                                tankPool[i].fluidInc = 0;
                            }
                            int incAdd = _split_inc(tankPool[i].fluidInc, count);
                            inc += incAdd;
                            tankPool[i].fluidInc -= incAdd;
                        }
                    }
                }
            }
        }

        private void RegisterTrash()
        {
            TrashContainer container = tmpFactory.gameData.trashSystem.container;
            TrashObject[] trashObjs = container.trashObjPool;
            for (int i = 0; i < container.trashCursor; i++)
            {
                if (trashObjs[i].item > 0)
                {
                    producedQuota[trashObjs[i].item] += trashObjs[i].count;
                }
            }
        }

        private void ConsumeTrash()
        {
            TrashContainer container = tmpFactory.gameData.trashSystem.container;
            TrashObject[] trashObjs = container.trashObjPool;
            for (int i = 0; i < container.trashCursor; i++)
            {
                if (trashObjs[i].item > 0)
                {
                    int itemId = trashObjs[i].item;
                    if (servedQuota[itemId] > produced[itemId])
                    {
                        int count = Math.Min(trashObjs[i].count, servedQuota[itemId] - produced[itemId]);
                        produced[itemId] += count;
                        trashObjs[i].count -= count;
                        int incAdd = _split_inc(trashObjs[i].inc, count);
                        inc += incAdd;
                        trashObjs[i].inc -= incAdd;
                        if (trashObjs[i].count <= 0)
                        {
                            container.RemoveTrash(i);
                        }
                    }
                }
            }
        }

        private void ConsumeBuffer()
        {
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                if (servedQuota[i] > produced[i] && buffer[i] > 0)
                {
                    int count = Math.Min(buffer[i], servedQuota[i] - produced[i]);
                    produced[i] += count;
                    buffer[i] -= count;
                }
            }
        }

        private void RegisterFractionator()
        {
            FractionatorComponent[] pool = tmpFactory.factorySystem.fractionatorPool;
            for(int i = 0; i < tmpFactory.factorySystem.fractionatorCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].fluidId > 0)
                    {
                        int count = _positive(pool[i].fluidInputMax * 4 - pool[i].fluidInputCount);
                        servedQuota[pool[i].fluidId] += count;
                        sumSpray += count;
                        count = _positive(pool[i].fluidOutputMax - pool[i].fluidOutputCount);
                        producedQuota[pool[i].fluidId] += count;
                    }
                    if (pool[i].productId > 0)
                    {
                        producedQuota[pool[i].productId] += pool[i].productOutputCount;
                    }
                }
            }
        }

        private void ConsumeFractionator()
        {
            FractionatorComponent[] pool = tmpFactory.factorySystem.fractionatorPool;
            for (int i = 0; i < tmpFactory.factorySystem.fractionatorCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].fluidId > 0)
                    {
                        int itemId = pool[i].fluidId;
                        int count;
                        if (producedQuota[itemId] > served[itemId])
                        {
                            count = _positive(Math.Min(pool[i].fluidInputMax * 4 - pool[i].fluidInputCount, producedQuota[itemId] - served[itemId]));
                            served[itemId] += count;
                            pool[i].fluidInputCount += count;
                            pool[i].fluidInputCargoCount += .25f * count;
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                pool[i].fluidInputInc += count * INC_ABILITY;
                            }
                        }
                        if (servedQuota[itemId] > produced[itemId])
                        {
                            count = Math.Min(pool[i].fluidOutputCount, servedQuota[itemId] - produced[itemId]);
                            produced[itemId] += count;
                            pool[i].fluidOutputCount -= count;
                            int incAdd = _split_inc(pool[i].fluidOutputInc, count);
                            inc += incAdd;
                            pool[i].fluidOutputInc -= incAdd;
                        }
                        if (pool[i].fluidOutputCount >= pool[i].fluidOutputMax && buffer[pool[i].fluidId] < BUFFER_SIZE)
                        {
                            count = pool[i].fluidOutputCount - pool[i].fluidOutputMax + 1;
                            pool[i].fluidOutputCount -= count;
                            buffer[pool[i].fluidId] += count;
                            int incAdd = _split_inc(pool[i].fluidOutputInc, count);
                            inc += incAdd;
                            pool[i].fluidOutputInc -= inc;
                        }
                    }
                    if (pool[i].productId > 0)
                    {
                        int itemId = pool[i].productId;
                        if (servedQuota[itemId] > produced[itemId])
                        {
                            int count = Math.Min(pool[i].productOutputCount, servedQuota[itemId] - produced[itemId]);
                            produced[itemId] += count;
                            pool[i].productOutputCount -= count;
                        }
                    }
                }
            }
        }

        private void Spray()
        {
            if (inc < sumSpray * INC_ABILITY)
            {
                if (producedQuota[PROLIFERATOR_MK3] > served[PROLIFERATOR_MK3])
                {
                    int count = Math.Min(
                        (sumSpray * INC_ABILITY - inc - 1) / (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) / INC_ABILITY + 1
                        , producedQuota[PROLIFERATOR_MK3] - served[PROLIFERATOR_MK3]);
                    inc += count * (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) * INC_ABILITY;
                    servedQuota[PROLIFERATOR_MK3] += count;
                    served[PROLIFERATOR_MK3] += count;
                    consumedProliferator = count;
                }
            }
            if (inc < 1)
            {
                spray = false;
            }
        }

        private static Func<int, int, int> _split_inc =
            (int inc, int count) => count <=0 || inc <= 0 ? 0 : inc / count * count;

        private static Func<float, float, bool> _float_equal = (float x, float y) => Math.Abs(x - y) < 0.001;

        private static Func<int, int> _positive = (int x) => x < 0 ? 0 : x;

        static Wormhole()
        {
            ItemProto proto = LDB.items.Select(PROLIFERATOR_MK3);
            INC_SPRAY_TIMES = proto.HpMax;
            INC_ABILITY = proto.Ability;
            EXTRA_INC_SPRAY_TIMES = (int)(INC_SPRAY_TIMES * (Cargo.incTable[INC_ABILITY] * 0.001) + 0.1);
        }
    }
}
