using System;
using static PlanetWormhole.Constants;
using static PlanetWormhole.Util.Functions;

namespace PlanetWormhole.Data
{
    internal class Cosmic
    {
        public int[] trashProduced;
        public int[] trashServed;
        public int[] stationProduced;
        public int[] stationServed;
        public GameData data;
        private Random rng;
        public static System.Threading.Mutex mutex = new System.Threading.Mutex();

        public Cosmic()
        {
            trashProduced = new int[MAX_ITEM_COUNT];
            trashServed = new int[MAX_ITEM_COUNT];
            stationProduced = new int[MAX_ITEM_COUNT];
            stationServed = new int[MAX_ITEM_COUNT];
            rng = new Random();
        }

        public void BeforeLocal()
        {
            Reset();
            if (PlanetWormhole.EnableInterstellar())
            {
                RegisterStation();
                ConsumeStation();
            }
            RegisterTrash();
        }

        public void AfterLocal()
        {
            ConsumeTrash();
        }

        public void Reset()
        {
            Array.Clear(trashProduced, 0, MAX_ITEM_COUNT);
            Array.Clear(trashServed, 0, MAX_ITEM_COUNT);
            Array.Clear(stationProduced, 0, MAX_ITEM_COUNT);
            Array.Clear(stationServed, 0, MAX_ITEM_COUNT);
        }

        public void SetData(GameData data)
        {
            this.data = data;
        }

        public void RegisterTrash()
        {
            TrashContainer container = data.trashSystem.container;
            TrashObject[] trashObjs = container.trashObjPool;
            for (int i = 0; i < container.trashCursor; i++)
            {
                if (trashObjs[i].item > 0)
                {
                    trashProduced[trashObjs[i].item] += trashObjs[i].count;
                }
            }
        }

        public void ConsumeTrash()
        {
            TrashContainer container = data.trashSystem.container;
            TrashObject[] trashObjs = container.trashObjPool;
            for (int i = 0; i < container.trashCursor; i++)
            {
                if (trashObjs[i].item > 0)
                {
                    int itemId = trashObjs[i].item;
                    if (trashServed[itemId] > 0)
                    {
                        int count = Math.Min(trashObjs[i].count, trashServed[itemId]);
                        trashServed[itemId] -= count;
                        trashObjs[i].count -= count;
                        if (trashObjs[i].count <= 0)
                        {
                            container.RemoveTrash(i);
                        }
                    }
                }
            }
        }

        public void RegisterStation()
        {
            for (int k = 0; k < data.factoryCount; k++)
            {
                StationComponent[] pool = data.factories[k].transport.stationPool;
                for (int i = 1; i < data.factories[k].transport.stationCursor; i++)
                {
                    if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                    {
                        StationStore[] storage = pool[i].storage;
                        for (int j = 0; j < storage.Length; j++)
                        {
                            if (storage[j].itemId > 0)
                            {
                                if (storage[j].remoteLogic == ELogisticStorage.Supply)
                                {
                                    stationProduced[storage[j].itemId] += storage[j].count;
                                }
                                else if (storage[j].remoteLogic == ELogisticStorage.Demand)
                                {
                                    int count = _positive(storage[j].max - storage[j].count);
                                    stationServed[storage[j].itemId] += count;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ConsumeStation()
        {
            uint u = (uint) rng.Next();
            int count = 0;
            for (int h = 0; h < data.factoryCount; h++)
            {
                int k = (int)((u + h) % data.factoryCount);
                StationComponent[] pool = data.factories[k].transport.stationPool;
                for (int i = 1; i < data.factories[k].transport.stationCursor; i++)
                {
                    if (pool[i] != null && pool[i].id == i && pool[i].storage != null)
                    {
                        StationStore[] storage = pool[i].storage;
                        for (int j = 0; j < storage.Length; j++)
                        {
                            if (storage[j].itemId > 0)
                            {
                                int itemId = storage[j].itemId;
                                if (storage[j].remoteLogic == ELogisticStorage.Supply)
                                {
                                    _produce(itemId, stationServed, ref storage[j].count, ref count);
                                }
                                else if (storage[j].remoteLogic == ELogisticStorage.Demand)
                                {
                                    _serve(itemId, stationProduced, ref storage[j].count, storage[j].max, ref count);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
