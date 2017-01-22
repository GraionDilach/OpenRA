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
            Utility.BotDebug("Initializing CabalBase at {0},{1}", conyard.Location.X, conyard.Location.Y);
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
        
    }
}
