using System;

namespace PlanetWormhole.Util
{
    internal class Functions
    {
        public static int _split_inc(int inc, int count)
        {
            return count <= 0 || inc <= 0 ? 0 : inc / count * count;
        }

        public static bool _float_equal(float x, float y)
        {
            return Math.Abs(x - y) < 0.001;
        }

        public static int _positive(int x)
        {
            return x < 0 ? 0 : x;
        }

        public static void _produce(int itemId, int[] served, ref int produce, ref int count)
        {
            if (served[itemId] > 0)
            {
                count = Math.Min(produce, served[itemId]);
                served[itemId] -= count;
                produce -= count;
            } else
            {
                count = 0;
            }
        }

        public static void _serve(int itemId, int[] produced
            , ref int serve, int max, ref int count)
        {
            if (produced[itemId] > 0)
            {
                count = _positive(Math.Min(max - serve, produced[itemId]));
                produced[itemId] -= count;
                serve += count;
            }
            else
            {
                count = 0;
            }
        }
    }
}
