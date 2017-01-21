using System.Collections.Generic;

namespace OpenRA.Mods.Common.AI.Cabal
{
    /// <summary>
    /// OrderManager which queues all orders given by the AI and then dequeues them when Tick is called
    /// </summary>
    public class CabalOrderManager
    {

        private readonly Queue<Order> _orders = new Queue<Order>();
        private readonly World _world;


        public CabalOrderManager(World world)
        {
            _world = world;
        }

        //currently not implemented order strings
        public static string Harvest = "Harvest";
        public static string Move = "Move";
        public static string RepairBuilding = "RepairBuilding";
        public static string SetRallyPoint = "SetRallyPoint";
        public static string PlaceBuilding = "PlaceBuilding";

        /// <summary>
        /// triggers the DeployTransform order on the given actor
        /// </summary>
        /// <param name="actor">the target to call DeployTransform on</param>
        /// <param name="queued"></param>
        public void Deploy(Actor actor, bool queued)
        {
            QueueOrder(new Order("DeployTransform", actor, queued));
        }

        public void QueueOrder(Order order)
        {
            _orders.Enqueue(order);
        }

        public void Tick()
        {
            var ordersToIssueThisTick = _orders.Count;
            for (var i = 0; i < ordersToIssueThisTick; i++)
                _world.IssueOrder(_orders.Dequeue());
        }
    }
}
