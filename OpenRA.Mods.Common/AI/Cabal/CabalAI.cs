using OpenRA.Mods.Common.AI.Cabal;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.AI
{


    public class CabalAI : IBot, ITick
    {
        public bool _enabled { get; private set; }

        public IBotInfo Info { get { return _info; } }

        public Player Player { get { return _player; } }

        public World World { get { return _world; } }

        public PowerManager Power { get { return _playerPower; } }
      
        readonly CabalAIInfo _info;
        private Player _player;
        private World _world;
        private PowerManager _playerPower;
        private PlayerResources _playerResource;
        private int currentTick = 0;
        private List<CabalBase> _bases = new List<CabalBase>();
        private Dictionary<ProductionQueue, CabalBase> _queueBaseAssignments = new Dictionary<ProductionQueue, CabalBase>();
        private CabalOrderManager _orderManager;
        private readonly IPathFinder pathfinder;

        //used to keep deployed mcv actors for location checks
        //to see where the deployed conyard should be
        //TODO: use for verification if a base lost its mcv to give it a new one
        private List<Actor> _newlyDeployedMcvs = new List<Actor>();

        public CabalAI(CabalAIInfo info, ActorInitializer init)
        {
            _info = info;
            _world = init.World;
            pathfinder = _world.WorldActor.Trait<IPathFinder>();
        }
        
        void IBot.Activate(Player player)
        {
            _player = player;
            _playerPower = player.PlayerActor.Trait<PowerManager>();
            _playerResource = player.PlayerActor.Trait<PlayerResources>();
            _orderManager = new CabalOrderManager(_world);
            _enabled = true;
        }

        
        
        public void Tick(Actor self)
        {
            if (!_enabled)
                return;

            //here is where we have to do stuff
            //Start every game by deploying the mcv might be a bit hacky but gets the job done
            if (currentTick == 1)
            {
                InitializeBase(self);
            }
            for(int i = 0; i < _newlyDeployedMcvs.Count(); i++)
            {
                //only check one successfull mcv per tick to prevent OutOfRange Execptions
                //when removing an mcv from the list
                if(CheckLocationForBase(_newlyDeployedMcvs[i]))
                {
                    break;
                }
            }


            _bases.ForEach(b => b.Tick());
            
            //Seras: trigger queued orders
            _orderManager.Tick();
            currentTick++;
        }

        /// <summary>
        /// Checks locations of deployed mcvs for creating a new base
        /// or adding the mcv to the already existing base in range
        /// </summary>
        /// <param name="mcv"></param>
        private bool CheckLocationForBase(Actor mcv)
        {
            var centerCell = _world.Map.CellContaining(mcv.CenterPosition);

            var actors = _world.FindActorsInCircle(mcv.CenterPosition, WDist.FromCells(3));
            var conyards = actors.Where(a => _info.BuildingCommonNames.ConstructionYard.Contains(a.Info.Name) && a.Owner == _player);
            if(conyards.Any())
            { 
                _bases.Add(new CabalBase(conyards.First(), _orderManager, _info, _world, _player));
                _newlyDeployedMcvs.Remove(mcv);
                return true;
            }
            return false;
        }

        private void deployBase(Actor mcv)
        {
            _orderManager.Deploy(mcv, false);
            _newlyDeployedMcvs.Add(mcv);
        }

        void InitializeBase(Actor self)
        {
            // Find and deploy our mcv
            var mcv = self.World.Actors.FirstOrDefault(a => a.Owner == _player &&
                _info.UnitsCommonNames.Mcv.Contains(a.Info.Name));

            if (mcv != null)
            {
                Cabal.Utility.BotDebug("Deploying initial MCV.");
                deployBase(mcv);
            }
            else
                Cabal.Utility.BotDebug("Can't find MCV.");
        }

    }
}
