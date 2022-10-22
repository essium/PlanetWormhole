using System;
using System.Threading;
using static PlanetWormhole.Constants;
using static PlanetWormhole.Util.Functions;

namespace PlanetWormhole.Data
{
    internal class LocalPlanet
    {
        public int[] produced;
        public int[] served;
        public int[] buffer;
        public int inc;
        public int consumedProliferator;
        public int sumSpray;
        public bool spray;
        public PlanetFactory factory;
        public Cosmic cosmic;
        public AutoResetEvent completeSignal;
        private uint r;

        private static ThreadLocal<Random> rng = new ThreadLocal<Random>(() => new Random());

        public LocalPlanet()
        {
            produced = new int[MAX_ITEM_COUNT];
            served = new int[MAX_ITEM_COUNT];
            buffer = new int[MAX_ITEM_COUNT];
            Array.Clear(buffer, 0, MAX_ITEM_COUNT);
            inc = 0;
            completeSignal = new AutoResetEvent(false);
        }

        public void SetFactory(PlanetFactory factory)
        {
            this.factory = factory;
        }

        public void SetCosmic(Cosmic cosmic)
        {
            this.cosmic = cosmic;
        }
        public void PatchPlanet(object stateInfo = null)
        {
            Reset();
            RegisterPowerSystem();
            RegisterMiner();
            RegisterAssembler();
            RegisterFractionator();
            RegisterLab();
            RegisterEjector();
            RegisterSilo();
            RegisterStorage();
            RegisterStation();
            Spray();
            ConsumeBuffer();
            ConsumeTrash();
            ConsumeStorage();
            ConsumePowerSystem();
            ConsumeMiner();
            ConsumeFractionator();
            ConsumeAssembler();
            ConsumeLab();
            ConsumeEjector();
            ConsumeSilo();
            ConsumeStation();
            completeSignal.Set();
        }

        private void Reset()
        {
            Array.Clear(produced, 0, MAX_ITEM_COUNT);
            Array.Clear(served, 0, MAX_ITEM_COUNT);
            spray = true;
            sumSpray = 0;
            consumedProliferator = 0;
            r = (uint) rng.Value.Next();
        }

        private void RegisterAssembler()
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
                            produced[pool[i].products[j]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].requireCounts.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(3 * pool[i].requireCounts[j] - pool[i].served[j]);
                            sumSpray += count;
                            served[pool[i].needs[j]] += count;
                        }
                    }
                }
            }
        }

        private void ConsumeAssembler()
        {
            AssemblerComponent[] pool = factory.factorySystem.assemblerPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.assemblerCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.assemblerCursor - 1)) + 1;
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    for (int j = 0; j < pool[i].produced.Length; j++)
                    {
                        if (pool[i].produced[j] > 0)
                        {
                            int itemId = pool[i].products[j];
                            _produce(itemId, served, ref pool[i].produced[j], ref count);
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
                            _serve(itemId, produced, ref pool[i].served[j], 3 * pool[i].requireCounts[j], ref count);
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

        private void RegisterStation()
        {
            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 1; i < factory.transport.stationCursor; i++)
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
                                produced[storage[j].itemId] += storage[j].count;
                            }
                            else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                int count = _positive(storage[j].max - storage[j].count);
                                served[storage[j].itemId] += count;
                            }
                        }
                    }
                    if (pool[i].needs[5] == WARPER && factory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        served[WARPER] += _positive(pool[i].warperMaxCount - pool[i].warperCount);
                    }
                }
            }
        }

        private void ConsumeStation()
        {
            StationComponent[] pool = factory.transport.stationPool;
            int count = 0;
            for (int k = 1; k < factory.transport.stationCursor; k++)
            {
                int i = (int)((r + k) % (factory.transport.stationCursor - 1)) + 1;
                if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                {
                    if (pool[i].needs[5] == WARPER && factory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        _serve(WARPER, produced, ref pool[i].warperCount, pool[i].warperMaxCount, ref count);
                    }
                }
            }
            for (int k = 1; k < factory.transport.stationCursor; k++)
            {
                int i = (int)((r + k) % (factory.transport.stationCursor - 1)) + 1;
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
                                _produce(itemId, served, ref storage[j].count, ref count);
                                int incAdd = _split_inc(storage[j].inc, count);
                                storage[j].inc -= incAdd;
                                inc += incAdd;
                            } else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                _serve(itemId, produced, ref storage[j].count, storage[j].max, ref count);
                            }
                        }
                    }
                }
            }
        }

        private void RegisterPowerSystem()
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            for (int i = 1; i < factory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && factory.gameData.history.TechUnlocked(IONOSPHERIC_TECH))
                    {
                        int count = _positive((72000 - pool[i].catalystPoint) / 3600);
                        sumSpray += count;
                        served[pool[i].catalystId] += count;
                        if (pool[i].productId > 0)
                        {
                            produced[pool[i].productId] += (int) pool[i].productCount;
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
                        served[itemId] += count;
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
                        produced[excPool[i].fullId] += excPool[i].fullCount;
                        served[excPool[i].emptyId] += _positive(PowerExchangerComponent.maxCount - excPool[i].emptyCount);
                    } else if (_float_equal(excPool[i].targetState, -1.0f))
                    {
                        produced[excPool[i].emptyId] += excPool[i].emptyCount;
                        served[excPool[i].fullId] += _positive(PowerExchangerComponent.maxCount - excPool[i].fullCount);
                    }
                }
            }
        }

        private void ConsumePowerSystem()
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            int count = 0;
            for (int k = 1; k < factory.powerSystem.genCursor; k++)
            {
                int i = (int)((r + k) % (factory.powerSystem.genCursor - 1)) + 1;
                if (pool[i].id == i)
                {
                    if (pool[i].catalystId > 0 && factory.gameData.history.TechUnlocked(IONOSPHERIC_TECH))
                    {
                        int itemId = pool[i].catalystId;
                        if (produced[itemId] > 0)
                        {
                            count = _positive(Math.Min((72000 - pool[i].catalystPoint) / 3600, produced[itemId]));
                            produced[itemId] -= count;
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
                            if (served[itemId] > 0)
                            {
                                count = Math.Min((int)pool[i].productCount, served[itemId]);
                                served[itemId] -= count;
                                pool[i].productCount -= count;
                            }
                        }
                    }
                    if (pool[i].fuelId > 0)
                    {
                        int itemId = pool[i].fuelId;
                        if (produced[itemId] > 0)
                        {
                            count = _positive(Math.Min(10 - pool[i].fuelCount, produced[itemId]));
                            produced[itemId] -= count;
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
            PowerExchangerComponent[] excPool = factory.powerSystem.excPool;
            for (int k = 1; k < factory.powerSystem.excCursor; k++)
            {
                int i = (int)((r + k) % (factory.powerSystem.excCursor - 1)) + 1;
                if (excPool[i].id == i && excPool[i].fullId > 0 && excPool[i].emptyId > 0)
                {
                    int fullIndex = excPool[i].fullId;
                    int emptyIndex = excPool[i].emptyId;
                    if (_float_equal(excPool[i].targetState, 1.0f))
                    {
                        if (served[fullIndex] > 0)
                        {
                            count = Math.Min(excPool[i].fullCount, served[fullIndex]);
                            served[fullIndex] -= count;
                            excPool[i].fullCount -= (short) count;
                        }
                        if (produced[emptyIndex] > 0)
                        {
                            count = _positive(Math.Min(PowerExchangerComponent.maxCount - excPool[i].emptyCount, produced[emptyIndex]));
                            produced[emptyIndex] -= count;
                            excPool[i].emptyCount += (short) count;
                        }
                    }
                    else if (_float_equal(excPool[i].targetState, -1.0f))
                    {
                        if (served[emptyIndex] > 0)
                        {
                            count = Math.Min(excPool[i].emptyCount, served[emptyIndex]);
                            served[emptyIndex] -= count;
                            excPool[i].emptyCount -= (short)count;
                        }
                        if (produced[fullIndex] > 0)
                        {
                            count = _positive(Math.Min(PowerExchangerComponent.maxCount - excPool[i].fullCount, produced[fullIndex]));
                            produced[fullIndex] -= count;
                            excPool[i].fullCount += (short)count;
                        }
                    }
                }
            }
        }

        private void RegisterMiner()
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            for (int i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        produced[pool[i].productId] += pool[i].productCount;
                    }
                }
            }
        }

        private void ConsumeMiner()
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.minerCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.minerCursor - 1)) + 1;
                if (pool[i].id == i)
                {
                    if (pool[i].productId > 0)
                    {
                        int itemId = pool[i].productId;
                        _produce(itemId, served, ref pool[i].productCount, ref count);
                    }
                }
            }
        }

        private void RegisterLab()
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
                            produced[pool[i].products[j]] += pool[i].produced[j];
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int count = _positive(4 - pool[i].served[j]);
                            sumSpray += count;
                            served[pool[i].needs[j]] += count;
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
                            served[pool[i].needs[j]] += count;
                        }
                    }
                }
            }
        }
        

        private void ConsumeLab()
        {
            LabComponent[] pool = factory.factorySystem.labPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.labCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.labCursor - 1)) + 1;
                if (pool[i].id == i && !pool[i].researchMode && pool[i].recipeId > 0)
                {
                    if (pool[i].productCounts != null && pool[i].productCounts.Length > 0)
                    {
                        for (int j = 0; j < pool[i].productCounts.Length; j++)
                        {
                            int itemId = pool[i].products[j];
                            _produce(itemId, served, ref pool[i].produced[j], ref count);
                        }
                    }
                    for (int j = 0; j < pool[i].needs.Length; j++)
                    {
                        if (pool[i].needs[j] > 0)
                        {
                            int itemId = pool[i].needs[j];
                            _serve(itemId, produced, ref pool[i].served[j], 4, ref count);
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                pool[i].incServed[j] += INC_ABILITY * count;
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
                            if (produced[itemId] > 0)
                            {
                                count = _positive(Math.Min((36000 - pool[i].matrixServed[j]) / 3600, produced[itemId]));
                                produced[itemId] -= count;
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
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            for (int i = 1; i < factory.factorySystem.ejectorCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    served[pool[i].bulletId] += count;
                }
            }
        }

        private void ConsumeEjector()
        {
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.ejectorCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.ejectorCursor - 1)) + 1;
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemId = pool[i].bulletId;
                    _serve(itemId, produced, ref pool[i].bulletCount, 20, ref count);
                    if (spray)
                    {
                        inc -= count * INC_ABILITY;
                        pool[i].bulletInc += INC_ABILITY * count;
                    }
                }
            }
        }

        private void RegisterSilo()
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            for (int i = 1; i < factory.factorySystem.siloCursor; i++)
            {
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int count = _positive(20 - pool[i].bulletCount);
                    sumSpray += count;
                    served[pool[i].bulletId] += count;
                }
            }
        }

        private void ConsumeSilo()
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.siloCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.siloCursor - 1)) + 1;
                if (pool[i].id == i && pool[i].bulletId > 0)
                {
                    int itemId = pool[i].bulletId;
                    _serve(itemId, produced, ref pool[i].bulletCount, 20, ref count);
                    if (spray)
                    {
                        inc -= count * INC_ABILITY;
                        pool[i].bulletInc += INC_ABILITY * count;
                    }
                }
            }
        }

        private void RegisterStorage()
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
                            produced[storagePool[i].grids[j].itemId] += storagePool[i].grids[j].count;
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
                        produced[tankPool[i].fluidId] += tankPool[i].fluidCount;
                    }
                }
            }
        }

        private void ConsumeStorage()
        {
            StorageComponent[] storagePool = factory.factoryStorage.storagePool;
            int count = 0;
            for (int k = 1; k < factory.factoryStorage.storageCursor; k++)
            {
                int i = (int)((r + k) % (factory.factoryStorage.storageCursor - 1)) + 1;
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
                            _produce(itemId, served, ref storagePool[i].grids[j].count, ref count);
                            int incAdd = _split_inc(storagePool[i].grids[j].inc, count);
                            inc += incAdd;
                            storagePool[i].grids[j].inc -= incAdd;
                            if (storagePool[i].grids[j].count <= 0)
                            {
                                storagePool[i].grids[j].itemId = 0;
                                storagePool[i].grids[j].count = 0;
                                storagePool[i].grids[j].inc = 0;
                            }
                            if (count > 0)
                            {
                                change = true;
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
            for (int k = 1; k < factory.factoryStorage.tankCursor; k++)
            {
                int i = (int)((r + k) % (factory.factoryStorage.tankCursor - 1)) + 1;
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        int itemId = tankPool[i].fluidId;
                        _produce(itemId, served, ref tankPool[i].fluidCount, ref count);
                        int incAdd = _split_inc(tankPool[i].fluidInc, count);
                        inc += incAdd;
                        tankPool[i].fluidInc -= incAdd;
                        if (tankPool[i].fluidCount <= 0)
                        {
                            tankPool[i].fluidId = 0;
                            tankPool[i].fluidCount = 0;
                            tankPool[i].fluidInc = 0;
                        }
                    }
                }
            }
        }

        private void ConsumeTrash()
        {
            int count = 0;
            Cosmic.mutex.WaitOne();
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                if (served[i] > 0)
                {
                    count = _positive(Math.Min(cosmic.trashProduced[i] - cosmic.trashServed[i], served[i]));
                    served[i] -= count;
                    cosmic.trashServed[i] += count;
                }
            }
            Cosmic.mutex.ReleaseMutex();
        }

        private void ConsumeBuffer()
        {
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                if (served[i] > 0 && buffer[i] > 0)
                {
                    int count = Math.Min(buffer[i], served[i]);
                    served[i] -= count;
                    buffer[i] -= count;
                }
            }
        }

        private void RegisterFractionator()
        {
            FractionatorComponent[] pool = factory.factorySystem.fractionatorPool;
            for(int i = 1; i < factory.factorySystem.fractionatorCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].fluidId > 0)
                    {
                        int count = _positive(pool[i].fluidInputMax * 4 - pool[i].fluidInputCount);
                        served[pool[i].fluidId] += count;
                        sumSpray += count;
                        count = _positive(pool[i].fluidOutputMax - pool[i].fluidOutputCount);
                        produced[pool[i].fluidId] += count;
                    }
                    if (pool[i].productId > 0)
                    {
                        produced[pool[i].productId] += pool[i].productOutputCount;
                    }
                }
            }
        }

        private void ConsumeFractionator()
        {
            FractionatorComponent[] pool = factory.factorySystem.fractionatorPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.fractionatorCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.fractionatorCursor - 1)) + 1;
                if (pool[i].id == i)
                {
                    if (pool[i].fluidId > 0)
                    {
                        int itemId = pool[i].fluidId;
                        _serve(itemId, produced, ref pool[i].fluidInputCount, 4 * pool[i].fluidInputMax, ref count);
                        pool[i].fluidInputCargoCount += .25f * count;
                        if (spray)
                        {
                            inc -= count * INC_ABILITY;
                            pool[i].fluidInputInc += count * INC_ABILITY;
                        }
                        _produce(itemId, served, ref pool[i].fluidOutputCount, ref count);
                        int incAdd = _split_inc(pool[i].fluidOutputInc, count);
                        inc += incAdd;
                        pool[i].fluidOutputInc -= incAdd;
                        if (pool[i].fluidOutputCount >= pool[i].fluidOutputMax && buffer[pool[i].fluidId] < BUFFER_SIZE)
                        {
                            count = pool[i].fluidOutputCount - pool[i].fluidOutputMax + 1;
                            pool[i].fluidOutputCount -= count;
                            buffer[pool[i].fluidId] += count;
                            incAdd = _split_inc(pool[i].fluidOutputInc, count);
                            inc += incAdd;
                            pool[i].fluidOutputInc -= inc;
                        }
                    }
                    if (pool[i].productId > 0)
                    {
                        int itemId = pool[i].productId;
                        _produce(itemId, served, ref pool[i].productOutputCount, ref count);
                    }
                }
            }
        }

        private void Spray()
        {
            if (inc < sumSpray * INC_ABILITY)
            {
                if (produced[PROLIFERATOR_MK3] > 0)
                {
                    int count = Math.Min(
                        (sumSpray * INC_ABILITY - inc - 1) / (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) / INC_ABILITY + 1
                        , produced[PROLIFERATOR_MK3]);
                    inc += count * (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) * INC_ABILITY;
                    produced[PROLIFERATOR_MK3] -= count;
                    served[PROLIFERATOR_MK3] += count;
                    consumedProliferator += count;
                }
            }
            if (inc < 1)
            {
                spray = false;
            }
        }
    }
}
