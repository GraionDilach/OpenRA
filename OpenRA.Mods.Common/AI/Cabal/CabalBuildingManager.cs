using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.AI.Cabal
{
    public class CabalBuildingManager
    {
        private readonly CabalOrderManager _orderManager;
        private readonly CabalAIInfo _aiInfo;
        private readonly World _world;
        private readonly IPathFinder _pathfinder;
        private readonly ResourceLayer _resLayer;
        private readonly Player _player;

        private readonly WPos _initialBaseCenterWPos;
        private readonly CPos _initialBaseCenterCPos;
        private readonly int _maxBaseRadius = 25;


        private Actor _conyard;
        private CabalQueueHandler _buildingQueue = null;
        private List<Actor> _resourceSeeders = new List<Actor>();

        //private ProductionQueue _defenseQueue = null;

        public CabalBuildingManager(Actor conyard, CabalOrderManager orderManager, CabalAIInfo aiInfo, World world, Player player)
        {
            _conyard = conyard;
            _orderManager = orderManager;
            _aiInfo = aiInfo;
            _world = world;
            _resLayer = world.WorldActor.Trait<ResourceLayer>();
            _initialBaseCenterWPos = _conyard.CenterPosition;
            _initialBaseCenterCPos = _conyard.Location;
            _pathfinder = _world.WorldActor.Trait<IPathFinder>();
            _player = player;
            AssingConyardQueues(conyard);
            _resourceSeeders = FindResourcesSeedersInCircleAroundBase(WDist.FromCells(_maxBaseRadius)).ToList();
        }

        public void Tick(int currentTick)
        {
            if(currentTick % 5 == 0)
            {
                _buildingQueue.Tick();
            }
        }


        public void AssingConyardQueues(Actor conyard)
        {
            Utility.BotDebug("Assigning build queues");
            var productionQueues = conyard.TraitsImplementing<ProductionQueue>();
            _buildingQueue = new CabalQueueHandler(_orderManager, productionQueues.First(pq => pq.Info.Group == QueueGroupNames.Building), ChooseBuildingToBuild, BuildingConstructionFinished);
            // _defenseQueue = productionQueues.First(pq => pq.Info.Group == QueueGroupNames.Defense);
        }

        private IEnumerable<Actor> FindResourcesSeedersInCircleAroundBase(WDist radius)
        {
            
            var resourceSeeders = _world.FindActorsInCircle(_initialBaseCenterWPos, radius).Where(a => a.Info.HasTraitInfo<SeedsResourceInfo>());
            return resourceSeeders.OrderBy(a => (a.Location - _initialBaseCenterCPos).Length).ToList();
        }

        private ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
        {
            Utility.BotDebug("New Construction Options");

            var constructionOptions = queue.BuildableItems();

            var power = constructionOptions.Where(info => _aiInfo.BuildingCommonNames.Power.Contains(info.Name)).FirstOrDefault();
            var refinery = constructionOptions.Where(info => _aiInfo.BuildingCommonNames.Refinery.Contains(info.Name)).FirstOrDefault();
            var normalStructures = constructionOptions.Where(info => !_aiInfo.BuildingCommonNames.Refinery.Contains(info.Name) && !_aiInfo.BuildingCommonNames.Power.Contains(info.Name));

            ActorInfo buildingToBuild = null;

            /*if(remainingPower < totalPower * 0.2)
            {
                return power;
            }*/
            CPos? preferredRefLocation = ChooseRefineryLocation();

            if(preferredRefLocation != null && refinery.TraitInfo<BuildingInfo>().IsCloseEnoughToBase(_world, _player, refinery.Name, preferredRefLocation.Value))
            {
                return refinery;
            }

            buildingToBuild = normalStructures.Random(Game.CosmeticRandom);



            if (buildingToBuild != null)
            {
                Utility.BotDebug("Building: {0}", buildingToBuild.Name);
                return buildingToBuild;
            }

            return null;
        }

        /// <summary>
        /// function to choose the a proper location for the next refinery to be build
        /// will be used to define basecrawling direction and to prevent building
        /// a other building on that space
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private CPos? ChooseRefineryLocation()
        {
            if(_buildingQueue == null)
            {
                return null;
            }
            var refineryInfo = _buildingQueue.GetBuildabeItems().First(ai => _aiInfo.BuildingCommonNames.Refinery.Contains(ai.Name));
            BuildingInfo buildingInfo = refineryInfo.TraitInfoOrDefault<BuildingInfo>();

            var harvesterActor = _world.Map.Rules.Actors.FirstOrDefault(ai => ai.Value.HasTraitInfo<HarvesterInfo>());
            var mobileInfo = harvesterActor.Value.TraitInfoOrDefault<MobileInfo>();
            //closestFreeResourceGenerator.

            var resourceSeeder = _resourceSeeders.FirstOrDefault();

            if(resourceSeeder == null)
            {
                Utility.BotDebug("No more resources available for a new Refinery");
                return null;
            }

            var path = _pathfinder.FindPath(PathSearch.FromPoint(_world, mobileInfo, _conyard, _initialBaseCenterCPos, resourceSeeder.Location, false));
            CPos firstCellWithoutResources = resourceSeeder.Location;
            foreach(CPos cell in path)
            {
                if(_resLayer.GetResource(cell) == null)
                {
                    var possibleCell = CheckBuildlocationIntersectingCell(refineryInfo.Name, buildingInfo, cell);
                    if (possibleCell != null)
                        return possibleCell;
                }
            }

            return null;
        }

        public CPos? CheckBuildlocationIntersectingCell(string name, BuildingInfo buildingInfo, CPos targetCell, int radius = 0)
        {
            //TODO: implement radius
            for (int x = buildingInfo.Dimensions.X; x >= 0; x--)
            {
                for (int y = buildingInfo.Dimensions.Y; y >= 0; y--)
                {
                    CPos currentCell = new CPos(targetCell.X + x, targetCell.Y + y);
                    if (_world.CanPlaceBuilding(name, buildingInfo, currentCell, null))
                    {
                        return currentCell;
                    }
                }
            }
            return null;
        }

        private void BuildingConstructionFinished(ProductionQueue queue, ProductionItem item)
        {
            if(_aiInfo.BuildingCommonNames.Refinery.Contains(item.Item))
            {
                Utility.BotDebug("placing refinery");
                _orderManager.PlaceBuilding(_player.PlayerActor, ChooseRefineryLocation().Value, item, queue);
            }
        }
    }
}
