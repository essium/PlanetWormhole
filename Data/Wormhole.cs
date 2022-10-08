using System;
using System.Collections.Generic;

namespace PlanetWormhole.Data
{
    internal class Wormhole
    {
        private static Dictionary<int, int> itemId2Index;
        private static int N;
        private static int PROLIF_MK3_INDEX;
        private static int INC_SPRAY_TIMES;
        private static int INC_ABILITY;
        private static int EXTRA_INC_SPRAY_TIMES;
        private int[] producedQuota;
        private int[] servedQuota;
        private int[] produced;
        private int[] served;
        private int inc;
        private int sumSpray;
        private bool spray;

        public Wormhole()
        {
            producedQuota = new int[N];
            servedQuota = new int[N];
            produced = new int[N];
            served = new int[N];
            inc = 0;
        }
        public void Patch(PlanetFactory factory)
        {
            Reset();
            RegisterPowerSystem(factory);
            RegisterMiner(factory);
            RegisterAssembler(factory);
            RegisterLab(factory);
            RegisterEjector(factory);
            RegisterSilo(factory);
            RegisterStorage(factory);
            RegisterStation(factory);
            Spray();
            ConsumeStorage(factory);
            ConsumePowerSystem(factory);
            ConsumeMiner(factory);
            ConsumeAssembler(factory);
            ConsumeLab(factory);
            ConsumeEjector(factory);
            ConsumeSilo(factory);
            ConsumeStation(factory);
        }

        private void Reset()
        {
            Array.Clear(producedQuota, 0, N);
            Array.Clear(servedQuota, 0, N);
            Array.Clear(produced, 0, N);
            Array.Clear(served, 0, N);
            spray = true;
            sumSpray = 0;
        }

        private void RegisterAssembler(PlanetFactory factory)
        {
            AssemblerComponent[] pool = factory.factorySystem.assemblerPool;
            for (int i = 1; i < factory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            producedQuota[itemId2Index[pool[i].products[j]]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(3 * pool[i].requireCounts[j] - pool[i].served[j]);
                            sumSpray += count;
                            servedQuota[itemId2Index[pool[i].needs[j]]] += count;
                        }
                    }
                }
            }
        }

        private void ConsumeAssembler(PlanetFactory factory)
        {
            AssemblerComponent[] pool = factory.factorySystem.assemblerPool;
            for (int i = 1; i < factory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0 && pool[i].produced.Length > 1)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            int itemIndex = itemId2Index[pool[i].products[j]];
                            if (servedQuota[itemIndex] > produced[itemIndex])
                            {
                                int count = Math.Min(pool[i].produced[j], servedQuota[itemIndex] - produced[itemIndex]);
                                produced[itemIndex] += count;
                                pool[i].produced[j] -= count;
                            }
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemIndex = itemId2Index[pool[i].needs[j]];
                            if (producedQuota[itemIndex] > served[itemIndex])
                            {
                                int count = _positive(Math.Min(3 * pool[i].requireCounts[j] - pool[i].served[j], producedQuota[itemIndex] - served[itemIndex]));
                                served[itemIndex] += count;
                                pool[i].served[j] += count;
                                if (spray)
                                {
                                    inc -= count;
                                    pool[i].incServed[j] += INC_ABILITY * count;
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 1; i < factory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0 && pool[i].produced.Length == 1)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            int itemIndex = itemId2Index[pool[i].products[j]];
                            if (servedQuota[itemIndex] > produced[itemIndex])
                            {
                                int count = Math.Min(pool[i].produced[j], servedQuota[itemIndex] - produced[itemIndex]);
                                produced[itemIndex] += count;
                                pool[i].produced[j] -= count;
                            }
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemIndex = itemId2Index[pool[i].needs[j]];
                            if (producedQuota[itemIndex] > served[itemIndex])
                            {
                                int count = _positive(Math.Min(3 * pool[i].requireCounts[j] - pool[i].served[j], producedQuota[itemIndex] - served[itemIndex]));
                                served[itemIndex] += count;
                                pool[i].served[j] += count;
                                if (spray)
                                {
                                    inc -= count;
                                    pool[i].incServed[j] += INC_ABILITY * count;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterStation(PlanetFactory factory)
        {
            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 1; i < factory.transport.stationCursor; i++)
            {
                if (pool[i].id == i && pool[i].storage != null)
                {
                    StationStore[] storage = pool[i].storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                producedQuota[itemId2Index[storage[j].itemId]] += storage[j].count;
                            }
                            else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                int count = _positive(storage[j].max - storage[j].count);
                                servedQuota[itemId2Index[storage[j].itemId]] += count;
                            }
                        }
                    }
                    if (pool[i].needs[5] == Constants.WARPER && factory.gameData.history.TechUnlocked(Constants.SHIP_ENGINE_4))
                    {
                        servedQuota[itemId2Index[Constants.WARPER]] += _positive(pool[i].warperMaxCount - pool[i].warperCount);
                    }
                }
            }
        }

        private void ConsumeStation(PlanetFactory factory)
        {
            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 1; i < factory.transport.stationCursor; i++)
            {
                if (pool[i].id == i && pool[i].storage != null)
                {
                    if (pool[i].needs[5] == Constants.WARPER && factory.gameData.history.TechUnlocked(Constants.SHIP_ENGINE_4))
                    {
                        int warperIndex = itemId2Index[Constants.WARPER];
                        if (producedQuota[warperIndex] > served[warperIndex])
                        {
                            int count = _positive(Math.Min(pool[i].warperMaxCount - pool[i].warperCount, producedQuota[warperIndex] - served[warperIndex]));
                            served[warperIndex] += count;
                            pool[i].warperCount += count;
                        }
                    }
                }
            }
            for (int i = 1; i < factory.transport.stationCursor; i++)
            {
                if (pool[i].id == i && pool[i].storage != null)
                {
                    StationStore[] storage = pool[i].storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            int itemIndex = itemId2Index[storage[j].itemId];
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                if (servedQuota[itemIndex] > produced[itemIndex])
                                {
                                    int count = Math.Min(storage[j].count, servedQuota[itemIndex] - produced[itemIndex]);
                                    produced[itemIndex] += count;
                                    storage[j].count -= count;
                                    int incAdd = _split_inc(storage[j].inc, count);
                                    storage[j].inc -= incAdd;
                                    inc += incAdd;
                                }
                            } else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                if (producedQuota[itemIndex] > served[itemIndex])
                                {
                                    int count = _positive(Math.Min(producedQuota[itemIndex] - served[itemIndex], storage[j].max - storage[j].count));
                                    served[itemIndex] += count;
                                    storage[j].count += count;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterPowerSystem(PlanetFactory factory)
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            for (int i = 1; i < factory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && factory.gameData.history.TechUnlocked(Constants.IONOSPHERIC_TECH))
                    {
                        int count = _positive((72000 - pool[i].catalystPoint) / 3600);
                        sumSpray += count;
                        servedQuota[itemId2Index[pool[i].catalystId]] += count;
                        if (pool[i].productId > 0)
                        {
                            producedQuota[itemId2Index[pool[i].productId]] += (int) pool[i].productCount;
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
                        servedQuota[itemId2Index[itemId]] += count;
                    }
                }
            }
            PowerExchangerComponent[] excPool = factory.powerSystem.excPool;
            for (int i = 1; i < factory.powerSystem.excCursor; i++)
            {
                if (excPool[i].id == i && excPool[i].fullId > 0 && excPool[i].emptyId > 0)
                {
                    if (_float_equal(excPool[i].targetState, 1.0f))
                    {
                        producedQuota[itemId2Index[excPool[i].fullId]] += excPool[i].fullCount;
                        servedQuota[itemId2Index[excPool[i].emptyId]] += _positive(PowerExchangerComponent.maxCount - excPool[i].emptyCount);
                    } else if (_float_equal(excPool[i].targetState, -1.0f))
                    {
                        producedQuota[itemId2Index[excPool[i].fullId]] += excPool[i].emptyCount;
                        servedQuota[itemId2Index[excPool[i].emptyId]] += _positive(PowerExchangerComponent.maxCount - excPool[i].fullCount);
                    }
                }
            }
        }

        private void ConsumePowerSystem(PlanetFactory factory)
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            for (int i = 1; i < factory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && factory.gameData.history.TechUnlocked(Constants.IONOSPHERIC_TECH))
                    {
                        int itemIndex = itemId2Index[pool[i].catalystId];
                        if (producedQuota[itemIndex] > served[itemIndex])
                        {
                            int count = _positive(Math.Min((72000 - pool[i].catalystPoint) / 3600, producedQuota[itemIndex] - served[itemIndex]));
                            served[itemIndex] += count;
                            pool[i].catalystPoint += count * 3600;
                            if (spray)
                            {
                                inc -= count;
                                pool[i].catalystIncPoint += count * 3600 * INC_ABILITY;
                            }
                        }
                        if (pool[i].productId > 0)
                        {
                            itemIndex = itemId2Index[pool[i].productId];
                            if (servedQuota[itemIndex] > produced[itemIndex])
                            {
                                int count = Math.Min((int)pool[i].productCount, servedQuota[itemIndex] - produced[itemIndex]);
                                produced[itemIndex] += count;
                                pool[i].productCount -= count;
                            }
                        }
                    }
                    if (pool[i].fuelId > 0)
                    {
                        int itemIndex = itemId2Index[pool[i].fuelId];
                        if (producedQuota[itemIndex] > served[itemIndex])
                        {
                            int count = _positive(Math.Min(10 - pool[i].fuelCount, producedQuota[itemIndex] - served[itemIndex]));
                            served[itemIndex] += count;
                            pool[i].fuelCount += (short) count;
                            if (spray)
                            {
                                inc -= count;
                                pool[i].fuelInc += (short) (INC_ABILITY * count);
                            }
                        }
                    }
                }
            }
            PowerExchangerComponent[] excPool = factory.powerSystem.excPool;
            for (int i = 1; i < factory.powerSystem.excCursor; i++)
            {
                if (excPool[i].id == i && excPool[i].fullId > 0 && excPool[i].emptyId > 0)
                {
                    int fullIndex = itemId2Index[excPool[i].fullId];
                    int emptyIndex = itemId2Index[excPool[i].emptyId];
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

        private void RegisterMiner(PlanetFactory factory)
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            for (int i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        producedQuota[itemId2Index[pool[i].productId]] += pool[i].productCount;
                    }
                }
            }
        }

        private void ConsumeMiner(PlanetFactory factory)
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            for (int i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        int itemIndex = itemId2Index[pool[i].productId];
                        if (servedQuota[itemIndex] > produced[itemIndex])
                        {
                            int count = Math.Min(pool[i].productCount, servedQuota[itemIndex] - produced[itemIndex]);
                            produced[itemIndex] += count;
                            pool[i].productCount -= count;
                        }
                    }
                }
            }
        }

        private void RegisterLab(PlanetFactory factory)
        {
            LabComponent[] pool = factory.factorySystem.labPool;
            for (int i = 1; i < factory.factorySystem.labCursor; i++)
            {
                if (pool[i].id == i && !pool[i].researchMode && pool[i].recipeId > 0)
                {
                    if (pool[i].productCounts != null && pool[i].productCounts.Length > 0)
                    {
                        for (int j = 0; j < pool[i].productCounts.Length; j++)
                        {
                            producedQuota[itemId2Index[pool[i].products[j]]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(4 - pool[i].served[j]);
                            sumSpray += count;
                            servedQuota[itemId2Index[pool[i].needs[j]]] += count;
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
                            servedQuota[itemId2Index[pool[i].needs[j]]] += count;
                        }
                    }
                }
            }
        }
        

        private void ConsumeLab(PlanetFactory factory)
        {
            LabComponent[] pool = factory.factorySystem.labPool;
            for (int i = 1; i < factory.factorySystem.labCursor; i++)
            {
                if (pool[i].id == i && !pool[i].researchMode && pool[i].recipeId > 0)
                {
                    if (pool[i].productCounts != null && pool[i].productCounts.Length > 0)
                    {
                        for (int j = 0; j < pool[i].productCounts.Length; j++)
                        {
                            int itemIndex = itemId2Index[pool[i].products[j]];
                            if (servedQuota[itemIndex] > produced[itemIndex])
                            {
                                int count = Math.Min(pool[i].produced[j], servedQuota[itemIndex] - produced[itemIndex]);
                                produced[itemIndex] += count;
                                pool[i].produced[j] -= count;
                            }
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemIndex = itemId2Index[pool[i].needs[j]];
                            if (producedQuota[itemIndex] > served[itemIndex])
                            {
                                int count = _positive(Math.Min(4 - pool[i].served[j], producedQuota[itemIndex] - served[itemIndex]));
                                served[itemIndex] += count;
                                pool[i].served[j] += count;
                                if (spray)
                                {
                                    inc -= count;
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
                            int itemIndex = itemId2Index[pool[i].needs[j]];
                            if (producedQuota[itemIndex] > served[itemIndex])
                            {
                                int count = _positive(Math.Min((36000 - pool[i].matrixServed[j]) / 3600, producedQuota[itemIndex] - served[itemIndex]));
                                served[itemIndex] += count;
                                pool[i].matrixServed[j] += count * 3600;
                                if (spray)
                                {
                                    inc -= count;
                                    pool[i].matrixIncServed[j] += INC_ABILITY * count * 3600;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterEjector(PlanetFactory factory)
        {
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            for (int i = 1; i < factory.factorySystem.ejectorCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    servedQuota[itemId2Index[pool[i].bulletId]] += count;
                }
            }
        }

        private void ConsumeEjector(PlanetFactory factory)
        {
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            for (int i = 1; i < factory.factorySystem.ejectorCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemIndex = itemId2Index[pool[i].bulletId];
                    if (producedQuota[itemIndex] > served[itemIndex])
                    {
                        int count = _positive(Math.Min(20 - pool[i].bulletCount, producedQuota[itemIndex] - served[itemIndex]));
                        served[itemIndex] += count;
                        pool[i].bulletCount += count;
                        if (spray)
                        {
                            inc -= count;
                            pool[i].bulletInc += INC_ABILITY * count;
                        }
                    }
                }
            }
        }

        private void RegisterSilo(PlanetFactory factory)
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            for (int i = 1; i < factory.factorySystem.siloCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    servedQuota[itemId2Index[pool[i].bulletId]] += count;
                }
            }
        }

        private void ConsumeSilo(PlanetFactory factory)
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            for (int i = 1; i < factory.factorySystem.siloCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemIndex = itemId2Index[pool[i].bulletId];
                    if (producedQuota[itemIndex] > served[itemIndex])
                    {
                        int count = _positive(Math.Min(20 - pool[i].bulletCount, producedQuota[itemIndex] - served[itemIndex]));
                        served[itemIndex] += count;
                        pool[i].bulletCount += count;
                        if (spray)
                        {
                            inc -= count;
                            pool[i].bulletInc += INC_ABILITY * count;
                        }
                    }
                }
            }
        }

        private void RegisterStorage(PlanetFactory factory)
        {
            StorageComponent[] storagePool = factory.factoryStorage.storagePool;
            for (int i = 1; i < factory.factoryStorage.storageCursor; i++)
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
                            producedQuota[itemId2Index[storagePool[i].grids[j].itemId]] += storagePool[i].grids[j].count;
                        }
                    }
                }
            }
            TankComponent[] tankPool = factory.factoryStorage.tankPool;
            for (int i = 1; i < factory.factoryStorage.tankCursor; i++)
            {
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        producedQuota[itemId2Index[tankPool[i].fluidId]] += tankPool[i].fluidCount;
                    }
                }
            }
        }

        private void ConsumeStorage(PlanetFactory factory)
        {
            StorageComponent[] storagePool = factory.factoryStorage.storagePool;
            for (int i = 1; i < factory.factoryStorage.storageCursor; i++)
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
                            int itemIndex = itemId2Index[storagePool[i].grids[j].itemId];
                            if (servedQuota[itemIndex] > produced[itemIndex])
                            {
                                int count = Math.Min(storagePool[i].grids[j].count, servedQuota[itemIndex] - produced[itemIndex]);
                                produced[itemIndex] += count;
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
            TankComponent[] tankPool = factory.factoryStorage.tankPool;
            for (int i = 1; i < factory.factoryStorage.tankCursor; i++)
            {
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        int itemIndex = itemId2Index[tankPool[i].fluidId];
                        if (servedQuota[itemIndex] > produced[itemIndex])
                        {
                            int count = Math.Min(tankPool[i].fluidCount, servedQuota[itemIndex] - produced[itemIndex]);
                            produced[itemIndex] += count;
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

        private void Spray()
        {
            if (inc < sumSpray)
            {
                if (producedQuota[PROLIF_MK3_INDEX] > served[PROLIF_MK3_INDEX])
                {
                    int count = Math.Min(
                        (sumSpray - inc - 1) / (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) + 1, producedQuota[PROLIF_MK3_INDEX] - served[PROLIF_MK3_INDEX]);
                    inc += count * (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1);
                    servedQuota[PROLIF_MK3_INDEX] += count;
                    served[PROLIF_MK3_INDEX] += count;
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
            itemId2Index = new Dictionary<int, int>();
            ItemProto[] items = LDB.items.dataArray;
            N = items.Length;
            for (int i = 0; i < N; i++)
            {
                itemId2Index.Add(items[i].ID, i);
            }
            ItemProto proto = LDB.items.Select(Constants.PROLIFERATOR_MK3);
            INC_SPRAY_TIMES = proto.HpMax;
            INC_ABILITY = proto.Ability;
            EXTRA_INC_SPRAY_TIMES = (int)(INC_SPRAY_TIMES * (Cargo.incTable[INC_ABILITY] * 0.001) + 0.1);
            PROLIF_MK3_INDEX = itemId2Index[Constants.PROLIFERATOR_MK3];
        }
    }
}
