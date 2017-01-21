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

        private readonly CabalOrderManager _orderManager;
        private readonly CabalAIInfo _aiInfo;
        private readonly World _world;
        private readonly Player _player;

        private WPos _defenseCenter;
        private WPos _initialBaseCenter;
        

        private CabalQueueHandler _buildingQueue = null;
        private ProductionQueue _defenseQueue = null;
        private List<ProductionQueue> _unitQueues = new List<ProductionQueue>();

        private List<Actor> _buildings = new List<Actor>();
        private List<Actor> Units = new List<Actor>();
        private int _currentTick = 0;
        private int _baseRemainingPower = 0;
        private int _baseTotalPower = 0;
        private int _maxBaseRadius = 25;

        public CabalBase(Actor conyard, CabalOrderManager orderManager, CabalAIInfo info, World world, Player player)
        {
            Utility.BotDebug("Initializing CabalBase");
            _initialBaseCenter = conyard.CenterPosition;
            _defenseCenter = conyard.CenterPosition;
            _orderManager = orderManager;
            _aiInfo = info;
            _world = world;
            _player = player;
            AssingConyardQueues(conyard);
            _buildings.Add(conyard);
            _baseRemainingPower = conyard.Info.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount);
            _baseTotalPower = _baseRemainingPower;
        }

        public void AssingConyardQueues(Actor conyard)
        {
            Utility.BotDebug("Assigning build queues");
            var productionQueues = conyard.TraitsImplementing<ProductionQueue>();
            _buildingQueue = new CabalQueueHandler(_orderManager, productionQueues.First(pq => pq.Info.Group == "Building"), ChooseBuildingToBuild, BuildingConstructionFinished);
            _defenseQueue = productionQueues.First(pq => pq.Info.Group == "Defence");
        }
        
        public void Tick()
        {
            if (_currentTick % 5 == 0)
            {
                CalculatePowerLevel();
                _buildingQueue.Tick();
            }

            _currentTick++;
        }

        //TODO:put into seperate class
        /// <summary>
        /// first dummy implementation on building selector we will see where it goes
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        private ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
        {
            Utility.BotDebug("Could create a new Building");

            ActorInfo buildingToBuild = null;
            UpdateBuildings();
            var buildableThings = queue.BuildableItems();

            //lets start simple and just allow it to start production of power plants
            var power = buildableThings.Where(info => _aiInfo.BuildingCommonNames.Power.Contains(info.Name)).FirstOrDefault();
            var refinery = buildableThings.Where(info => _aiInfo.BuildingCommonNames.Refinery.Contains(info.Name)).FirstOrDefault();
            var normalStructures = buildableThings.Where(info => !_aiInfo.BuildingCommonNames.Refinery.Contains(info.Name) && !_aiInfo.BuildingCommonNames.Power.Contains(info.Name));

            if(_currentTick >= 20 && !_buildings.Any(b => _aiInfo.BuildingCommonNames.Refinery.Contains(b.Info.Name)))
            {
                return refinery;
            }
            //if power is below 20% of total build power plant
            if (_baseRemainingPower < _baseTotalPower * 0.2)
            {
                return power;
            }
            if(_buildings.Count(a => _aiInfo.BuildingCommonNames.Production.Contains(a.Info.Name)) >= _buildings.Count(a => _aiInfo.BuildingCommonNames.Refinery.Contains(a.Info.Name)) * 3)
            {
                normalStructures = normalStructures.Where(ns => !_aiInfo.BuildingCommonNames.Production.Contains(ns.Name));
            }

            if(normalStructures.Any())
                buildingToBuild = normalStructures.ElementAt(Game.CosmeticRandom.Next(0, normalStructures.Count()));

            if(buildingToBuild != null && _buildings.Where(b => !_aiInfo.BuildingCommonNames.Power.Contains(b.Info.Name) && !_aiInfo.BuildingCommonNames.Refinery.Contains(b.Info.Name) && !_aiInfo.BuildingCommonNames.Production.Contains(b.Info.Name)).Any( b => b.Info.Name == buildingToBuild.Name))
            {
                Utility.BotDebug("Prevent building of another {0}", buildingToBuild.Name);
                buildingToBuild = null;
            }

            //TODO: proper detection
            if (buildingToBuild != null && buildingToBuild.HasTraitInfo<PowerInfo>() && _baseRemainingPower + buildingToBuild.TraitInfos<PowerInfo>().Where(ti => ti.EnabledByDefault).Sum(ti => ti.Amount) < 0)
            {
                Utility.BotDebug("Not enough power for {0}, building {1} instead", buildingToBuild.Name, power.Name);
                return power;
            }

            //TODO: force ai to have atleast one of every productionQueue Building so that it is able to build harvesters

            //TODO: if selected building is production building check refinery ratio
            if (buildingToBuild != null)
            { 
                Utility.BotDebug("Choose to build {0}", buildingToBuild.Name);
                return buildingToBuild;
            }

            Utility.BotDebug("Choose to build nothing");
            return null;
        }

        

        private void BuildingConstructionFinished(ProductionQueue queue, ProductionItem item)
        {
            CPos? buildingLocation = ChooseBuildLocation(item);
            if (buildingLocation.HasValue)
            {
                Utility.BotDebug("Building {0} at location {1},{2}", item.Item, buildingLocation.Value.X, buildingLocation.Value.Y);
                _orderManager.PlaceBuilding(_player.PlayerActor, buildingLocation.Value, item, queue);
            }
        }

        //really shitty build location logic but will get the job done for the first tests
        private CPos? ChooseBuildLocation(ProductionItem item)
        {
            bool isRefinery = _world.Map.Rules.Actors[item.Item].HasTraitInfo<RefineryInfo>();
            BuildingInfo buildingInfo = _world.Map.Rules.Actors[item.Item].TraitInfoOrDefault<BuildingInfo>();
            
            //probably shit code but first attempt
            CPos baseCenter = _world.Map.CellContaining(_initialBaseCenter);

            //TODO: make max base size changeable
            var cells = _world.Map.FindTilesInAnnulus(baseCenter, 3, _maxBaseRadius);

            cells = cells.Shuffle(Game.CosmeticRandom);

            //TODO: check if the building is a refinery if it is check for near by tibtree actors and try to place as close as possible to it

            foreach(CPos cell in cells)
            {
                if (_world.CanPlaceBuilding(item.Item, buildingInfo, cell, null))
                {
                    Utility.BotDebug("Found build location for {0}", item.Item);
                    return cell;
                }
            }

            Utility.BotDebug("No buildlocation for {0}", item.Item);
            return null;
        }

        private void UpdateBuildings()
        {
            //fast and hacky way for the first tests
            var  buildingActors = _world.ActorsHavingTrait<BuildingInfo>().Where(a => a.Owner == _player);
            var actorsAroundBase = _world.FindActorsInCircle(_initialBaseCenter, WDist.FromCells(_maxBaseRadius + 3));//just a little overhead to be save            
            _buildings = actorsAroundBase.Where(a => a.Info.HasTraitInfo<BuildingInfo>() && a.Owner == _player).ToList();
        }

        //put power into seperate class (CabalBasePowerManager)
        private void CalculatePowerLevel()
        {
            //shacky logic but should work for the start
            var powerInfos = _buildings.Where(b => b.Info.HasTraitInfo<PowerInfo>()).SelectMany(b => b.Info.TraitInfos<PowerInfo>().Where(ti => ti.EnabledByDefault));
            _baseRemainingPower = powerInfos.Sum(info => info.Amount);
            _baseTotalPower = powerInfos.Where(info => info.Amount > 0).Sum(info => info.Amount);
            //thats the base internal power logic AI should be able to override orders at a later point to make a better cross base decision

        }
    }
}
