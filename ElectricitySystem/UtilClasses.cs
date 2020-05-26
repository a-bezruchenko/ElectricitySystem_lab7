using System;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary.Map;
using System.Collections.Generic;


namespace lab7
{
    interface IElectriticyGridPart
    {
        Point getCoordinate();
        IElectriticyGridPart getElectriticySource();
        bool isBroken();
        void getFixed();
        String getName();
    }

    interface IElectriticyGiver : IElectriticyGridPart
    {
        // возвращаемое значение — количество реально отданной энергии
        double giveElectricity(double amount, long time);
    }



    // электростанция
    [CustomStyle(
        @"new ol.style.Style({
        image: new ol.style.Circle({
            opacity: 1.0,
            scale: 1.0,
            radius: 30,
            fill: new ol.style.Fill({
                color: 'rgba(255, 255, 0, 0.5)'
            }),
            stroke: new ol.style.Stroke({
                color: 'rgba(0, 0, 0, 0.4)',
                width: 1
            }),
        })
    });
    ")]
    class Generator : Point, IElectriticyGiver
    {
        private readonly double break_prob = 0.0;
        private readonly long tickLenght = 10;

        private double power = 1000000000.0;
        private double energyLeft;
        private long lastGeneratedTime;
        private bool is_broken;

        public Generator(Coordinate coordinate) : base(coordinate)
        {
            energyLeft = power;
            is_broken = false;
            lastGeneratedTime = 0;
        }

        public double giveElectricity(double amount, long time)
        {
            if (RandomGenerator.getRandomBool(break_prob))
            {
                is_broken = true;
                return 0;
            }
            else
            {
                if (time - lastGeneratedTime > tickLenght)
                {
                    //Console.WriteLine(String.Format("Used {0} energy units out of {1} ({2}%)", power - energyLeft, power, (power - energyLeft) * 100 / power));
                    energyLeft = power;
                    lastGeneratedTime = time;
                }

                double outputAmount = Math.Min(energyLeft, amount);
                energyLeft -= outputAmount;
                return outputAmount;
            }
        }

        public Point getCoordinate() => this;

        public bool isBroken() => is_broken;

        public void getFixed()
        {
            is_broken = false;
        }

        public IElectriticyGridPart getElectriticySource() => null;
        public String getName() => "Станция";
    }

    // электропровод
    [CustomStyle(
        @"new ol.style.Style({
            stroke: new ol.style.Stroke({
                color: 'black',
                width: 2
            })
        })"
        )]
    class Wire : LineString, IElectriticyGiver
    {
        private readonly double lossPerLenght = 0.01;
        private readonly double break_prob = 0.00001;
        private long lastTimeChecked = 0;

        private bool is_broken;
        private IElectriticyGiver parent;

        public string Name { get; private set; }

        public Wire(Coordinate[] points, IElectriticyGiver parent, String name) : base(points)
        {
            this.parent = parent;
            this.Name = name;
            is_broken = false;
        }

        double IElectriticyGiver.giveElectricity(double amount, long time)
        {
            if (is_broken)
                return 0;
            long timePassed = time - lastTimeChecked;
            double final_break_prob = 1 - Math.Pow(1 - break_prob, timePassed);

            lastTimeChecked = time;

            if (RandomGenerator.getRandomBool(final_break_prob))
            {
                is_broken = true;
                Console.WriteLine(String.Format("Провод {0} сломался", Name));
                return 0;
            }
            else
            {
                var totalLoss = lossPerLenght * Length;
                //Console.WriteLine(String.Format("Выдано {0} энергии", amount + totalLoss));
                return parent.giveElectricity(amount + totalLoss, time)- totalLoss;
            }
        }

        public IElectriticyGridPart getElectriticySource() => parent;

        public Point getCoordinate()
        {
            var start = Coordinates[0];
            var end = Coordinates[1];
            Coordinate result_coordinate = new Coordinate();
            result_coordinate.X = (start.X + end.X) / 2;
            result_coordinate.Y = (start.Y + end.Y) / 2;
            return new Point(result_coordinate);
        }

        public bool isBroken() => is_broken;

        public void getFixed()
        {
            is_broken = false;
        }
        public String getName() => Name;
    }

    [CustomStyle(
        @"new ol.style.Style({
        image: new ol.style.Circle({
            opacity: 1.0,
            scale: 1.0,
            radius: 4,
            fill: new ol.style.Fill({
                color: 'rgba(255, 0, 0, 0.8)'
            }),
            stroke: new ol.style.Stroke({
                color: 'rgba(0, 0, 0, 0.4)',
                width: 1
            }),
        })
    });
    ")]
    class Consumer : Point, IElectriticyGridPart
    {
        private readonly IElectriticyGiver energySource;
        private readonly double consumptionPerTick = 1;
        private long lastTimeConsumed = 0;
        private String name;

        public Consumer(Coordinate coordinate, IElectriticyGiver energySource, String name) : base(coordinate)
        {
            this.energySource = energySource;
            this.name = name;
        }

        public Consumer(Coordinate coordinate, IElectriticyGiver energySource, double consumptionPerTick) : base(coordinate)
        {
            this.energySource = energySource;
            this.consumptionPerTick = consumptionPerTick;
        }

        public bool Consume(long time)
        {
            double realConsumption = time - lastTimeConsumed;
            double energyGot = energySource.giveElectricity(realConsumption, time);
            lastTimeConsumed = time;
            //if (Math.Abs(realConsumption - energyGot)<0.001)
            //Console.WriteLine(String.Format("Потребитель {0} потребовал {1} энергии, получил {2}", name, realConsumption, energyGot));

            return Math.Abs(realConsumption - energyGot) < 0.001;
        }

        public Point getCoordinate() => this;

        public bool isBroken() => false;

        public void getFixed() {}

        public IElectriticyGridPart getElectriticySource() => energySource;

        public String getName() => name;
    }

    abstract class Mob : Point
    {
        protected readonly double speed;
        public List<Point> path;

        public Mob(Coordinate initial_coordinate, double speed) : base(initial_coordinate)
        {
            this.speed = speed;
            path = null;
        }

        public void MovePath()
        {
            if (path != null)
            {
                StepToPoint(path[0]);

                if (path[0].Distance(this) < 1 && path.Count != 1)
                    path.RemoveAt(0);
            }
        }

        protected void StepToPoint(Point point)
        {
            double xDistance = point.X - X;
            double yDistance = point.Y - Y;
            const double eps = 0.01;
            if (Math.Abs(xDistance) < eps && Math.Abs(yDistance) < eps)
                return;
            else if (Math.Abs(xDistance) > eps && Math.Abs(yDistance) < eps)
                X += speed * Math.Sign(xDistance);
            else if (Math.Abs(xDistance) < eps && Math.Abs(yDistance) > eps)
                Y += speed * Math.Sign(yDistance);
            else
            {
                double k = Math.Abs(xDistance) / (Math.Abs(yDistance) + Math.Abs(xDistance));
                X += Math.Min(Math.Abs(xDistance), speed * k) * Math.Sign(xDistance);
                Y += Math.Min(Math.Abs(yDistance), speed * (1 - k)) * Math.Sign(yDistance);
            }
        }

        public static bool HasNoTarget(Mob mob)
        {
            return mob.path == null;
        }
    }

    [CustomStyle(
    @"new ol.style.Style({
        image: new ol.style.Circle({
            opacity: 1.0,
            scale: 1.0,
            radius: 4,
            fill: new ol.style.Fill({
                color: 'rgba(255, 255, 0, 0.8)'
            }),
            stroke: new ol.style.Stroke({
                color: 'rgba(0, 0, 0, 0.4)',
                width: 1
            }),
        })
    });
    ")]
    class Electritian: Mob
    {
        public Consumer consumerBeingProcessed;
        public IElectriticyGridPart target;
        public Point home;
        public Electritian(Coordinate initial_coordinate, Point home) : base(initial_coordinate, 5.0)
        {
            target = null;
            consumerBeingProcessed = null;
            this.home = home;
        }

        public void setConsumerProcessed(Consumer consumerBeingProcessed)
        {
            this.consumerBeingProcessed = consumerBeingProcessed;
            setTarget(consumerBeingProcessed.getElectriticySource());
        }

        public void setTarget(IElectriticyGridPart target)
        {
            this.target = target;
            path = new List<Point> { target.getCoordinate() };
        }

        public void clearConsumerProcessed()
        {
            target = null;
            consumerBeingProcessed = null;
            path = new List<Point> { home };
        }
    }


    [CustomStyle(
    @"new ol.style.Style({
        image: new ol.style.Circle({
            opacity: 1.0,
            scale: 1.0,
            radius: 4,
            fill: new ol.style.Fill({
                color: 'rgba(128, 128, 0, 0.8)'
            }),
            stroke: new ol.style.Stroke({
                color: 'rgba(0, 0, 0, 0.4)',
                width: 1
            }),
        })
    });
    ")]
    class DispatherStation: Point
    {
        public Dictionary<Consumer, bool> consumersBeingProcessed;
        public List<Consumer> pendingRequests;

        public DispatherStation(Coordinate initial_coordinate) : base(initial_coordinate)
        {
            consumersBeingProcessed = new Dictionary<Consumer, bool>();
            pendingRequests = new List<Consumer>();
        }
    }

    static class RandomGenerator
    {
        private static Random RNG;

        static RandomGenerator()
        {
            RNG = new Random(42);
            Console.WriteLine("ГСЧ инициализирован.");
        }

        public static double getRandomDouble(double min, double max)
        {
            return RNG.NextDouble() * (max - min) + min;
        }

        public static Coordinate getRandomCoordinate(double x_min, double y_min, double x_max, double y_max)
        {
            double x = RNG.NextDouble() * (x_max - x_min) + x_min;
            double y = RNG.NextDouble() * (y_max - y_min) + y_min;
            return new Coordinate(x, y);
        }

        public static bool getRandomBool(double chanceForTrue)
        {
            if (chanceForTrue < 0)
                chanceForTrue = 0;
            if (chanceForTrue > 1)
                chanceForTrue = 1;
            if (RNG.NextDouble() <= chanceForTrue)
                return true;
            else
                return false;
        }

        public static int getRandomInt(int min, int max)
        {
            return RNG.Next(min, max);
        }
    }
}