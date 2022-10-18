namespace PlanetWormhole.Data
{
    internal class PlanetThreadObject
    {
        public Wormhole wormhole;
        public PlanetFactory factory;

        public PlanetThreadObject()
        {
            wormhole = new Wormhole();
        }

        public void SetFactory(PlanetFactory factory)
        {
            this.factory = factory;
        }
    }
}
