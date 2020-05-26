using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace lab7
{
    class ObjectGenerator
    {
        public static Generator GetGenerator()
        {
            return new Generator(new Coordinate(4971565, 6250411));
        }

        public static List<Wire> GetWires(Generator generator)
        {
            var c1 = new Coordinate(4971565, 6250411);
            var c2 = new Coordinate(4971565, 6252411);
            var c3 = new Coordinate(4969565, 6250411);
            var c4 = new Coordinate(4972565, 6252411);
            var c5 = new Coordinate(4970565, 6251411);
            var c6 = new Coordinate(4969965, 6250011);
            var c7 = new Coordinate(4969065, 6250011);

            var w1 = new Wire(new[] { c1, c2 }, generator, "1");
            var w2 = new Wire(new[] { c1, c3 }, generator, "2");
            var w3 = new Wire(new[] { c2, c4 }, w2, "3");
            var w4 = new Wire(new[] { c2, c5 }, w2, "4");
            var w5 = new Wire(new[] { c3, c6 }, w3, "5");
            var w6 = new Wire(new[] { c3, c7 }, w3, "6");

            return new List<Wire> { w1, w2, w3, w4, w5, w6 };
        }

        public static List<Consumer> GetConsumers(List<Wire> wires)
        {
            var res = new List<Consumer>();
            foreach (Wire wire in wires)
            {
                Coordinate wire_c = wire.getCoordinate().Coordinate;
                int amount = 1; //RandomGenerator.getRandomInt(2, 10);
                for (int i = 0; i < amount; i++)
                {
                    Coordinate c = wire_c.Copy();
                    c.X += RandomGenerator.getRandomInt(-50, 50);
                    c.Y += RandomGenerator.getRandomInt(-50, 50);
                    res.Add(new Consumer(c, wire, wire.Name));
                }
            }
            return res;
        }

        public static List<Electritian> GetElectritians()
        {
            var res = new List<Electritian>();
            Coordinate baseCoordinate = new Coordinate(4971615, 6250461);
            int amount = 5;//RandomGenerator.getRandomInt(2, 10);
            for (int i = 0; i < amount; i++)
            {
                var c = baseCoordinate.Copy();
                c.X += RandomGenerator.getRandomInt(-50, 50);
                c.Y += RandomGenerator.getRandomInt(-50, 50);
                res.Add(new Electritian(c, new Point(c.Copy())));
            }
            
            return res;
        }

        public static DispatherStation GetDispatherStation()
        {
            return new DispatherStation(new Coordinate(4971615, 6250461));
        }
    }
}
