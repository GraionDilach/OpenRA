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
        private WPos _defenseCenter;
        private WPos _initialBaseCenter;
        
        readonly Queue<Order> _orders = new Queue<Order>();

        private CabalQueueHandler _buildingQueue = null;
        private ProductionQueue _defenseQueue = null;
        private List<ProductionQueue> _unitQueues = new List<ProductionQueue>();

        private List<Actor> Buildings = new List<Actor>();
        private List<Actor> Units = new List<Actor>();
        private int _currentTick = 0;
        private readonly CabalOrderManager _orderManager;
        private readonly CabalAIInfo _aiInfo;

        public CabalBase(Actor conyard, CabalOrderManager orderManager, CabalAIInfo info)
        {
            Utility.BotDebug("Initializing CabalBase");
            _initialBaseCenter = conyard.CenterPosition;
            _defenseCenter = conyard.CenterPosition;
            _orderManager = orderManager;
            _aiInfo = info;
            AssingConyardQueues(conyard);
        }

        public void AssingConyardQueues(Actor conyard)
        {
            Utility.BotDebug("Assigning build queues");
            var productionQueues = conyard.TraitsImplementing<ProductionQueue>();
            _buildingQueue = new CabalQueueHandler(_orderManager, productionQueues.First(pq => pq.Info.Group == "Building"), ChooseBuildingToBuild, BuildingConstructionFinished);
            _defenseQueue = productionQueues.First(pq => pq.Info.Group == "Defence");
        }

        private void BuildingConstructionFinished(ProductionItem obj)
        {
            Utility.BotDebug("Finished construction of a Building");
        }

        private ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
        {
            Utility.BotDebug("Could create a new Building");

            ActorInfo buildingToBuild = null;

            var buildableThings = queue.BuildableItems();

            //lets start simple and just allow it to start production of power plants
            buildingToBuild = buildableThings.Where(info => _aiInfo.BuildingCommonNames.Power.Contains(info.Name)).FirstOrDefault();



            if (buildingToBuild != null)
            { 
                Utility.BotDebug("Choose to build {0}", buildingToBuild.Name);
                return buildingToBuild;
            }

            Utility.BotDebug("Choose to build nothing");
            return null;
        }

        public void Tick()
        {
            if(_currentTick % 5 == 0)
            {
                _buildingQueue.Tick();
            }
            _currentTick++;
        }
    }
}
