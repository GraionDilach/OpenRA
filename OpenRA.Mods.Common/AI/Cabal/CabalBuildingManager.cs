﻿using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Drawing;

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


        private int _baseRemainingPower = 0;
        private int _baseTotalPower = 0;
        private Actor _conyard;
        private CabalQueueHandler _buildingQueue = null;
        private Actor _resourceSeeder;
        private List<Actor> _usedResourceSeeders = new List<Actor>();
        private List<Actor> _buildings = new List<Actor>();
        private CPos? _currentRefineryTargetLocation;
        private ActorInfo _refineryInfo;
        private int _remainingTicksSinceLastConstructionOrder = 0; //calculate this value
        private int _orderDelay = 50;
        private Color _playerColor;

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
            _baseRemainingPower = conyard.Info.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount);
            _baseTotalPower = _baseRemainingPower;
            AssingConyardQueues(conyard);
            _resourceSeeder = FindResourcesSeedersInCircleAroundBase(WDist.FromCells(_maxBaseRadius), _initialBaseCenterWPos).First();
            Utility.BotDebug(_playerColor, "Found ResourceSeeder at {0},{1}", _resourceSeeder.Location.X, _resourceSeeder.Location.Y);
            _refineryInfo = _buildingQueue.GetBuildabeItems().Where(info => _aiInfo.BuildingCommonNames.Refinery.Contains(info.Name)).First();
            var newLogic = GetBuildLocationTowardsTarget(_resourceSeeder.Location, _initialBaseCenterCPos, _refineryInfo.TraitInfo<BuildingInfo>(), _refineryInfo.Name);
            _currentRefineryTargetLocation = GetBuildLocationTowardsTarget(_resourceSeeder.Location, _initialBaseCenterCPos, _refineryInfo.TraitInfo<BuildingInfo>(), _refineryInfo.Name);
            Utility.BotDebug(_playerColor, "RefineryTarget at {0},{1}", _currentRefineryTargetLocation.Value.X, _currentRefineryTargetLocation.Value.Y);
            _playerColor = player.Color.RGB;
        }

        public void Tick(int currentTick)
        {
            if(currentTick % 5 == 0)
            {
                UpdateBuildings();
                CalculateBasePowerLevel();
                if(_remainingTicksSinceLastConstructionOrder <= 0)
                {
                    _buildingQueue.Tick();
                }
                _remainingTicksSinceLastConstructionOrder--;
            }
        }

        public CPos? GetBuildLocationTowardsTarget(CPos target, CPos source, BuildingInfo buildingInfo, string name, bool checkIsCloseEnough = false, int border = 0, IEnumerable<CPos> disabledCells = null)
        {
            int maxDistance = Math.Abs((source - target).Length);
            for(int i = 0; i < maxDistance; i++)
            {
                var cellstoCheck = _world.Map.FindTilesInAnnulus(target, i, i + 1);

                var orderdCells = cellstoCheck.Select(c => new {
                    Position = c,
                    distanceSource = Math.Abs((source - c).Length),
                    distanceTarget = Math.Abs((c - target).Length),
                }).Where(c => (c.distanceSource + c.distanceTarget) > maxDistance).OrderBy(c => c.distanceSource + c.distanceTarget).ThenBy(c => c.distanceTarget);

                foreach(var cell in orderdCells)
                {
                    if(_world.CanPlaceBuilding(name, buildingInfo, cell.Position, null) && (!checkIsCloseEnough || buildingInfo.IsCloseEnoughToBase(_world, _player, name, cell.Position)))
                    {
                        var buildingCells = GetCellsFromBuildInfo(cell.Position, buildingInfo, border);
                        if (border > 0)
                        {
                            if (buildingCells.Any(bc => _world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(bc) != null))
                            {
                                continue;
                            }
                        }
                        if(disabledCells != null && buildingCells.Any(bc => disabledCells.Any(dc => dc.Equals(bc))))
                        {
                            continue;
                        }
                        
                        return cell.Position;
                        
                    }
                }
            }
            return null;
        }

        private IEnumerable<CPos> GetCellsFromBuildInfo(CPos location, BuildingInfo buildingInfo, int border = 0)
        {
            var blockedPositions = new List<CPos>();
            for (int x = border * -1; x < buildingInfo.Dimensions.X + border; x++)
            {
                for (int y = border * -1; y < buildingInfo.Dimensions.Y + border; y++)
                {
                    blockedPositions.Add(new CPos(location.X + x, location.Y + y));
                }
            }
            return blockedPositions;
        }


        public void AssingConyardQueues(Actor conyard)
        {
            //Utility.BotDebug("Assigning build queues");
            var productionQueues = conyard.TraitsImplementing<ProductionQueue>();
            _buildingQueue = new CabalQueueHandler(_orderManager, productionQueues.First(pq => pq.Info.Group == QueueGroupNames.Building), ChooseBuildingToBuild, BuildingConstructionFinished);
            // _defenseQueue = productionQueues.First(pq => pq.Info.Group == QueueGroupNames.Defense);
        }

        private IEnumerable<Actor> FindResourcesSeedersInCircleAroundBase(WDist radius, WPos height)
        {            
            var resourceSeeders = _world.FindActorsInCircle(_initialBaseCenterWPos, radius).Where(a => a.Info.HasTraitInfo<SeedsResourceInfo>());
            return resourceSeeders.Where(rs => Math.Abs(_initialBaseCenterWPos.Z - rs.CenterPosition.Z) <= 512 * 3 ) .OrderBy(a => Math.Abs((a.Location - _initialBaseCenterCPos).Length)).ToList();
        }

        private ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
        {
            var constructionOptions = queue.BuildableItems();

            var power = constructionOptions.Where(info => _aiInfo.BuildingCommonNames.Power.Contains(info.Name)).FirstOrDefault();
            var refinery = constructionOptions.Where(info => _aiInfo.BuildingCommonNames.Refinery.Contains(info.Name)).FirstOrDefault();
            var normalStructures = constructionOptions.Where(info => !_aiInfo.BuildingCommonNames.Refinery.Contains(info.Name) && !_aiInfo.BuildingCommonNames.Power.Contains(info.Name));

            ActorInfo buildingToBuild = null;
            _remainingTicksSinceLastConstructionOrder = _orderDelay;
            if (_baseRemainingPower < _baseTotalPower * 0.2)
            {
                return power;
            }

            if (_currentRefineryTargetLocation != null && _refineryInfo.TraitInfo<BuildingInfo>().IsCloseEnoughToBase(_world, _player, refinery.Name, _currentRefineryTargetLocation.Value))
            {
                return refinery;
            }

            buildingToBuild = normalStructures.Random(Game.CosmeticRandom);
            if (buildingToBuild != null)
            {
                Utility.BotDebug(_playerColor, "Building: {0}", buildingToBuild.Name);
                return buildingToBuild;
            }

            return null;
        }

        private void BuildingConstructionFinished(ProductionQueue queue, ProductionItem item)
        {
            _remainingTicksSinceLastConstructionOrder = _orderDelay;
            BuildingInfo buildingInfo = _world.Map.Rules.Actors[item.Item].TraitInfoOrDefault<BuildingInfo>();
            /*if(_resourceSeeder != null)
            {
                _currentRefineryTargetLocation = GetBuildLocationTowardsTarget(_resourceSeeder);
            }*/
            CPos? targetLocation = null;
            if (_aiInfo.BuildingCommonNames.Refinery.Contains(item.Item))
            {
                _currentRefineryTargetLocation = null;
                targetLocation = GetBuildLocationTowardsTarget(_resourceSeeder.Location,_initialBaseCenterCPos, buildingInfo, item.Item, true, 1);

                _usedResourceSeeders.Add(_resourceSeeder);
                _resourceSeeder = FindResourcesSeedersInCircleAroundBase(WDist.FromCells(_maxBaseRadius), _initialBaseCenterWPos).Where(rs => !_usedResourceSeeders.Contains(rs)).FirstOrDefault();
                if (_resourceSeeder != null)
                {
                    Utility.BotDebug(_playerColor, "Found ResourceSeeder at {0},{1}", _resourceSeeder.Location.X, _resourceSeeder.Location.Y);
                    _currentRefineryTargetLocation = GetBuildLocationTowardsTarget(_resourceSeeder.Location, _initialBaseCenterCPos, buildingInfo, item.Item, false, 1);
                    Utility.BotDebug(_playerColor, "RefineryTarget at {0},{1}", _currentRefineryTargetLocation.Value.X, _currentRefineryTargetLocation.Value.Y);
                }
            }
            else if (_currentRefineryTargetLocation.HasValue)
            {
                //TODO: basecrawl towards refinery location

                var harvesterActor = _world.Map.Rules.Actors.FirstOrDefault(ai => ai.Value.HasTraitInfo<HarvesterInfo>());
                var mobileInfo = harvesterActor.Value.TraitInfoOrDefault<MobileInfo>();
                var refineryBuildingInfo = _refineryInfo.TraitInfo<BuildingInfo>();

                var refineryCells = GetCellsFromBuildInfo(_currentRefineryTargetLocation.Value, refineryBuildingInfo, 1);
                targetLocation = GetBuildLocationTowardsTarget(_currentRefineryTargetLocation.Value, _initialBaseCenterCPos, buildingInfo, item.Item, true, 1, refineryCells);
            }
            else
            {
                //TODO: build at a random spot
                var cells = _world.Map.FindTilesInAnnulus(_initialBaseCenterCPos, 3, _maxBaseRadius);

                cells = cells.Shuffle(Game.CosmeticRandom);
                var cellsToCheck = cells.ToList();
                foreach (CPos cell in cellsToCheck)
                {
                    if (_world.CanPlaceBuilding(item.Item, buildingInfo, cell, null) && buildingInfo.IsCloseEnoughToBase(_world, _player, item.Item, cell))
                    {
                        //Utility.BotDebug("Found build location for {0}", item.Item);
                        targetLocation = cell;
                        break;
                    }
                }
            }
            if(targetLocation.HasValue)
            {

                Utility.BotDebug(_playerColor, "placing {0} at {1},{2}", item.Item, targetLocation.Value.X, targetLocation.Value.Y);
                _orderManager.PlaceBuilding(_player.PlayerActor, targetLocation.Value, item, queue);
            }
            else
            {
                Utility.BotDebug(_playerColor, "no building location for {0}", item.Item);
            }
        }

        private void UpdateBuildings()
        {
            //fast and hacky way for the first tests
            var buildingActors = _world.ActorsHavingTrait<BuildingInfo>().Where(a => a.Owner == _player);
            var actorsAroundBase = _world.FindActorsInCircle(_initialBaseCenterWPos, WDist.FromCells(_maxBaseRadius));//just a little overhead to be save            
            _buildings = actorsAroundBase.Where(a => a.Info.HasTraitInfo<BuildingInfo>() && a.Owner == _player).ToList();
        }

        private void CalculateBasePowerLevel()
        {
            //shacky logic but should work for the start
            var powerInfos = _buildings.Where(b => b.Info.HasTraitInfo<PowerInfo>()).SelectMany(b => b.Info.TraitInfos<PowerInfo>().Where(ti => ti.EnabledByDefault));
            _baseRemainingPower = powerInfos.Sum(info => info.Amount);
            _baseTotalPower = powerInfos.Where(info => info.Amount > 0).Sum(info => info.Amount);
            //thats the base internal power logic. AI should be able to override orders at a later point to make a better cross base decision

        }
    }
}
