using OpenRA.Mods.Common.Pathfinder;
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
        private List<ProductionQueue> _unitQueues = new List<ProductionQueue>();
        private List<Actor> _units = new List<Actor>();
        
        private int _currentTick = 0;
        private readonly CabalBuildingManager _BuildingManager;

        public CabalBase(Actor conyard, CabalOrderManager orderManager, CabalAIInfo aiInfo, World world, Player player)
        {
            Utility.BotDebug("Initializing CabalBase");
            //_initialBaseCenter = conyard.CenterPosition;
            //_defenseCenter = conyard.CenterPosition;
            _orderManager = orderManager;
            _aiInfo = aiInfo;
            _world = world;
            _player = player;
            //LocateResourceGenerators();
            //_baseRemainingPower = conyard.Info.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount);
            //_baseTotalPower = _baseRemainingPower;
            //_pathfinder = _world.WorldActor.Trait<IPathFinder>();
            _BuildingManager = new CabalBuildingManager(conyard, orderManager, aiInfo, world, player);
        }
        public void Tick()
        {
            _BuildingManager.Tick(_currentTick);
            _currentTick++;
        }

        /*
        private void LocateResourceGenerators()
        {
            var baseCenter = _world.Map.CellContaining(_initialBaseCenter);
            var resourceGenerators = _world.FindActorsInCircle(_initialBaseCenter, WDist.FromCells(_maxBaseRadius)).Where(a => a.Info.HasTraitInfo<SeedsResourceInfo>());
            _nearbyResourceGenerators = resourceGenerators.OrderBy(a => (a.Location-baseCenter).Length).ToList();
        }
        */



        //TODO:put into seperate class
        /// <summary>
        /// first dummy implementation on building selector we will see where it goes
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        /*private ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
        {
            Utility.BotDebug("Could create a new Building");

            ActorInfo buildingToBuild = null;
            UpdateBuildings();

            //used to basecrawl towards resources for better efficiency
            _buildTowardsResources = _nearbyResourceGenerators.Any(a => !_allocatedResourceGenerators.ContainsKey(a));

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
            if(_currentTick >= 20 && _buildings.Count(a => _aiInfo.BuildingCommonNames.Production.Contains(a.Info.Name)) >= _buildings.Count(a => _aiInfo.BuildingCommonNames.Refinery.Contains(a.Info.Name)) * 3)
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
            if(lastPlacementAttempt == 0)
            { 
                CPos? buildingLocation = ChooseBuildLocation(item);
                if (buildingLocation.HasValue)
                {
                    Utility.BotDebug("Building {0} at location {1},{2}", item.Item, buildingLocation.Value.X, buildingLocation.Value.Y);
                    _orderManager.PlaceBuilding(_player.PlayerActor, buildingLocation.Value, item, queue);
                }
                lastPlacementAttempt += 10;
            }
            lastPlacementAttempt--;
        }

        //really shitty build location logic but will get the job done for the first tests
        private CPos? ChooseBuildLocation(ProductionItem item)
        {
            bool isRefinery = _world.Map.Rules.Actors[item.Item].HasTraitInfo<RefineryInfo>();
            BuildingInfo buildingInfo = _world.Map.Rules.Actors[item.Item].TraitInfoOrDefault<BuildingInfo>();
            
            //probably shit code but first attempt
            CPos baseCenter = _world.Map.CellContaining(_initialBaseCenter);

            var closestFreeResourceGenerator = _nearbyResourceGenerators.FirstOrDefault(a => !_allocatedResourceGenerators.ContainsKey(a));

            List<CPos> cellsToCheck = null;
            //closestFreeResourceGenerator
            if (closestFreeResourceGenerator != null)
            {
                Utility.BotDebug("crawling toward resource");
                var harvesterActor = _world.Map.Rules.Actors.FirstOrDefault(ai => ai.Value.HasTraitInfo<HarvesterInfo>());
                var mobileInfo = harvesterActor.Value.TraitInfoOrDefault<MobileInfo>();
                //closestFreeResourceGenerator.
                var path = _pathfinder.FindPath(PathSearch.FromPoint(_world, mobileInfo, _conyard, _world.Map.CellContaining(_initialBaseCenter), closestFreeResourceGenerator.Location, false));
                cellsToCheck = path;
                //just a cheat to test basecrawling
                if(isRefinery)
                {
                    _allocatedResourceGenerators.Add(closestFreeResourceGenerator, null);
                }
            }
            else
            {
                Utility.BotDebug("choosing random spot");

                //TODO: make max base size changeable
                var cells = _world.Map.FindTilesInAnnulus(baseCenter, 3, _maxBaseRadius);

                cells = cells.Shuffle(Game.CosmeticRandom);
                cellsToCheck = cells.ToList();
            }
            foreach(CPos cell in cellsToCheck)
            {
                if (_world.CanPlaceBuilding(item.Item, buildingInfo, cell, null) && buildingInfo.IsCloseEnoughToBase(_world, _player, item.Item, cell))
                {
                    Utility.BotDebug("Found build location for {0}", item.Item);
                    return cell;
                }
            }
            

            Utility.BotDebug("No build location for {0}", item.Item);
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
            //thats the base internal power logic. AI should be able to override orders at a later point to make a better cross base decision

        }
        */
    }
}
