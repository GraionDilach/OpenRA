using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.AI.Cabal
{
    public class CabalBase
    {
        private WPos defenseCenter;
        private WPos initialBaseCenter;
        
        readonly Queue<Order> _orders = new Queue<Order>();

        private ProductionQueue _buildingQueue = null;
        private ProductionQueue _defenseQueue = null;
        private List<ProductionQueue> _unitQueues = new List<ProductionQueue>();

        private List<Actor> Buildings = new List<Actor>();
        private List<Actor> Units = new List<Actor>();
        private int _currentTick = 0;

        public CabalBase(Actor conyard)
        {
            Utility.BotDebug("Initializing CabalBase");
            initialBaseCenter = conyard.CenterPosition;
            defenseCenter = conyard.CenterPosition;
            AssingConyardQueues(conyard);
        }

        public void AssingConyardQueues(Actor conyard)
        {
            Utility.BotDebug("Assigning build queues");
            var productionQueues = conyard.TraitsImplementing<ProductionQueue>();
            _buildingQueue = productionQueues.First(pq => pq.Info.Group == "Building");
            _defenseQueue = productionQueues.First(pq => pq.Info.Group == "Defence");
        }

        public void Tick()
        {
            _currentTick++;
        }
    }
}
