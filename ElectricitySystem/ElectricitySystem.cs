using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary.Modules;

namespace lab7
{
    public class ElectricitySystem : OSMLSModule
    {
        protected override void Initialize()
        {
            var generator = ObjectGenerator.GetGenerator();
            var wires = ObjectGenerator.GetWires(generator);
            var consumers = ObjectGenerator.GetConsumers(wires);
            var elecritians = ObjectGenerator.GetElectritians();
            var dispatherStation = ObjectGenerator.GetDispatherStation();

            foreach (var el in wires)
                MapObjects.Add(el);

            foreach (var el in consumers)
                MapObjects.Add(el);

            foreach (var el in elecritians)
                MapObjects.Add(el);

            MapObjects.Add(generator);
            MapObjects.Add(dispatherStation);
        }

        public override void Update(long elapsedMilliseconds)
        {
            Generator generator = MapObjects.Get<Generator>()[0];
            DispatherStation dispatherStation = MapObjects.Get<DispatherStation>()[0];
            List<Wire> wires = MapObjects.GetAll<Wire>();
            List<Consumer> consumers = MapObjects.GetAll<Consumer>();
            List<Electritian> electritians = MapObjects.GetAll<Electritian>();
            
            List<Electritian> freeElectritians = electritians.Where(x => x.target == null).ToList();

            foreach (var consumer in consumers)
            {
                bool consumedSuccessfully = consumer.Consume(elapsedMilliseconds);
                bool consumerIsPending = dispatherStation.consumersBeingProcessed.ContainsKey(consumer) && dispatherStation.consumersBeingProcessed[consumer];
                if (!consumedSuccessfully && !consumerIsPending)
                {
                    Console.WriteLine(String.Format("Потребитель {0} отправил запрос", consumer.getName()));
                    dispatherStation.consumersBeingProcessed[consumer] = true;
                    dispatherStation.pendingRequests.Add(consumer);
                }
            }

            while (freeElectritians.Count > 0 && dispatherStation.pendingRequests.Count > 0)
            {
                freeElectritians[0].setConsumerProcessed(dispatherStation.pendingRequests[0]);
                Console.WriteLine(String.Format("Электрик отправился выполнять запрос {0}", dispatherStation.pendingRequests[0].getName()));
                freeElectritians.RemoveAt(0);
                dispatherStation.pendingRequests.RemoveAt(0);
            }

            foreach (Electritian e in electritians)
                e.MovePath();

            foreach (Electritian e in electritians.Where(x => x.target != null).ToList())
            {
                if (e.Distance(e.target.getCoordinate()) < 1)
                {
                    bool isFixed = false;
                    if (e.target.isBroken())
                    {
                        e.target.getFixed();
                        isFixed = true;
                        Console.WriteLine(String.Format("Провод {0} починен", e.target.getName()));
                    }
                    else
                    {
                        var source = e.target.getElectriticySource();
                        if (source == null)
                            isFixed = true;
                        else
                            e.setTarget(source);
                    }

                    if (isFixed)
                    {
                        dispatherStation.consumersBeingProcessed[e.consumerBeingProcessed] = false;
                        e.clearConsumerProcessed();

                    }
                }
            }



        }
    }
}