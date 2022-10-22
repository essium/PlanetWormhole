using BepInEx.Configuration;

namespace PlanetWormhole
{
    internal class Constants
    {
        public const int PROLIFERATOR_MK3 = 1143;
        public const int WARPER = 1210;
        public const int IONOSPHERIC_TECH = 1505;
        public const int SHIP_ENGINE_4 = 3404;
        public const int MAX_ITEM_COUNT = 12000;
        public const int BUFFER_SIZE = 1000;
        public const int PERIOD = 9;
        public static int INC_SPRAY_TIMES;
        public static int INC_ABILITY;
        public static int EXTRA_INC_SPRAY_TIMES;

        static Constants()
        {
            ItemProto proto = LDB.items.Select(PROLIFERATOR_MK3);
            INC_SPRAY_TIMES = proto.HpMax;
            INC_ABILITY = proto.Ability;
            EXTRA_INC_SPRAY_TIMES = (int)(INC_SPRAY_TIMES * (Cargo.incTable[INC_ABILITY] * 0.001) + 0.1);
        }
    }
}
