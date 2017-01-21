using OpenRA.Traits;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.AI
{
    public class CabalAIInfo : ITraitInfo, IBotInfo
    {
        [Desc("Ingame name this bot uses.")]
        public readonly string Name = "Seraphinexx Bot";

        string IBotInfo.Name { get { return Name; } }

        public object Create(ActorInitializer init) { return new CabalAI(this, init); }

        public class UnitCategories
        {
            public readonly HashSet<string> Mcv = new HashSet<string>();
            public readonly HashSet<string> ExcludeFromSquads = new HashSet<string>();
        }

        public class BuildingCategories
        {
            public readonly HashSet<string> ConstructionYard = new HashSet<string>();
            public readonly HashSet<string> VehiclesFactory = new HashSet<string>();
            public readonly HashSet<string> Refinery = new HashSet<string>();
            public readonly HashSet<string> Power = new HashSet<string>();
            public readonly HashSet<string> Barracks = new HashSet<string>();
            public readonly HashSet<string> Production = new HashSet<string>();
            public readonly HashSet<string> NavalProduction = new HashSet<string>();
            public readonly HashSet<string> Silo = new HashSet<string>();
        }

        //copied from Hacky AI
        #region common names for easier identification for same actors
        [Desc("Tells the AI what unit types fall under the same common name. Supported entries are Mcv and ExcludeFromSquads.")]
        [FieldLoader.LoadUsing("LoadUnitCategories", true)]
        public readonly UnitCategories UnitsCommonNames;

        [Desc("Tells the AI what building types fall under the same common name.",
            "Possible keys are ConstructionYard, Power, Refinery, Silo , Barracks, Production, VehiclesFactory, NavalProduction.")]
        [FieldLoader.LoadUsing("LoadBuildingCategories", true)]
        public readonly BuildingCategories BuildingCommonNames;
        #endregion

        #region resource checks
        [Desc("How many randomly chosen cells with resources to check when deciding refinery placement.")]
        public readonly int MaxResourceCellsToCheck = 3;
        #endregion

        static object LoadUnitCategories(MiniYaml yaml)
        {
            var categories = yaml.Nodes.First(n => n.Key == "UnitsCommonNames");
            return FieldLoader.Load<UnitCategories>(categories.Value);
        }

        static object LoadBuildingCategories(MiniYaml yaml)
        {
            var categories = yaml.Nodes.First(n => n.Key == "BuildingCommonNames");
            return FieldLoader.Load<BuildingCategories>(categories.Value);
        }
    }
}
