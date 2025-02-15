using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class GridScan
    {
        // working fields
        private readonly IMyGridTerminalSystem _gridTerminalSystem;
        private readonly IMyShipController _shipController;

        // cache fields
        private Dictionary<Base6Directions.Direction, List<IMyThrust>> _thrusterByDirection = null;
        private List<IMyLandingGear> _landingGears = null;

        public Dictionary<Base6Directions.Direction, List<IMyThrust>> ThrustersByDirection =>
            _thrusterByDirection ?? (_thrusterByDirection = GetThrusterByDirection());
        public List<IMyThrust> Thrusters =>
            (_thrusterByDirection ?? (_thrusterByDirection = GetThrusterByDirection()))?.Values.SelectMany(x => x).ToList();
        public List<IMyLandingGear> LandingGear => _landingGears ?? (_landingGears = GetLandingGears());
        
        public List<ControlGroup> ControlGroups { get; } = new List<ControlGroup>();

        public GridScan(IMyGridTerminalSystem gridTerminalSystem, params string[] controlGroupNames)
        {
            _gridTerminalSystem = gridTerminalSystem;

            // get the single main Controller of Grid
            List<IMyShipController> allShipControllers = new List<IMyShipController>();
            _gridTerminalSystem.GetBlocksOfType(allShipControllers);
            if (allShipControllers.Count != 1 && allShipControllers.TrueForAll(c => !c.IsMainCockpit))
                throw new Exception("There has to be exactly one Controller or a designated MainCockpit");
            _shipController = allShipControllers.Single(c => c.IsMainCockpit);

            // get control groups
            if (controlGroupNames != null && controlGroupNames.Length > 0)
            {
                foreach (string controlGroupName in controlGroupNames)
                {
                    List<IMyTerminalBlock> controlGroupBlocks = new List<IMyTerminalBlock>();
                    gridTerminalSystem.GetBlocksOfType(controlGroupBlocks,
                        b => b.CustomData.Contains("[" + controlGroupName + "]"));
                    ControlGroups.Add(new ControlGroup(controlGroupName, controlGroupBlocks));
                }
            }
        }


        private List<IMyLandingGear> GetLandingGears()
        {
            List<IMyLandingGear> landingGears = new List<IMyLandingGear>();

            //expand later to only get actual landing gear/filter out hangar gear or similar
            _gridTerminalSystem.GetBlocksOfType(landingGears);

            return landingGears;
        }

        private Dictionary<Base6Directions.Direction, List<IMyThrust>> GetThrusterByDirection()
        {
            Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusterByDiretion =
                new Dictionary<Base6Directions.Direction, List<IMyThrust>>();

            List<IMyThrust> allThrusters = new List<IMyThrust>();
            _gridTerminalSystem.GetBlocksOfType(allThrusters);

            foreach (Base6Directions.Direction direction in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                List<IMyThrust> sortedThruster = new List<IMyThrust>();
                foreach (IMyThrust thruster in allThrusters)
                {
                    if (_shipController.Orientation.TransformDirection(thruster.Orientation.Forward) == direction)
                    {
                        sortedThruster.Add(thruster);
                    }
                }

                thrusterByDiretion[direction] = sortedThruster;
            }

            return thrusterByDiretion;
        }
    }

    public class ControlGroup
    {
        private readonly List<IMyTerminalBlock> _blocks;
        public string Name { get; private set; }

        public Dictionary<IMyTerminalBlock, BlockConfig> Config { get; } =
            new Dictionary<IMyTerminalBlock, BlockConfig>();

        public ControlGroup(string name, List<IMyTerminalBlock> blocks)
        {
            Name = name;
            _blocks = blocks;
            blocks.ForEach(b => Config[b] = new BlockConfig(b));
        }
    }

    public class BlockConfig
    {
        public int Count => _settings.Count;
        public List<string> Names => _settings.Keys.Select(n => n.ToString()).ToList();

        private readonly Dictionary<string, IConfig> _settings = new Dictionary<string, IConfig>();

        public BlockConfig(IMyTerminalBlock block)
        {
            string[] lines = block.CustomData.Split('\n');
            foreach (string line in lines)
            {
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(line, @"^(\w+):(\w+)$");
                if (match.Success) _settings[match.Groups[1].Value] = ConfigFactory.Create(match.Groups[1].Value, match.Groups[2].Value);
            }
        }

        public Config<T> GetConfig<T>(string name)
        {
            return _settings[name] as Config<T>;
        }

        public IConfig GetConfig(string name)
        {
            return _settings[name];
        }
    }

    public static class ConfigFactory
    {
        public static IConfig Create(string name, string value)
        {
            int intValue;
            if (Int32.TryParse(value, out intValue))
            {
                return new Config<int>(name, intValue);
            }
            return new Config<string>(name, value);
        }
    }

    public class Config<T> : IConfig
    {
        public string Name { get; }

        public Type ValueType => typeof(T);
        // public Type ValueType => typeof(T);
        public T Value { get; }

        public Config(string name, T value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IConfig
    {
        string Name { get; }
        Type ValueType { get; }
    }
}