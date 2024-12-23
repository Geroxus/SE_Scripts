using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
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

        public Dictionary<Base6Directions.Direction, List<IMyThrust>> ThrusterByDirection => _thrusterByDirection ?? (_thrusterByDirection = GetThrusterByDirection());

        public GridScan(IMyGridTerminalSystem gridTerminalSystem)
        {
            _gridTerminalSystem = gridTerminalSystem;

            List<IMyShipController> allShipControllers = new List<IMyShipController>();
            _gridTerminalSystem.GetBlocksOfType(allShipControllers);
            if (allShipControllers.Count != 1 && allShipControllers.TrueForAll(c => !c.IsMainCockpit))
                throw new Exception("There has to be exactly one Controller or a designated MainCockpit");
            _shipController = allShipControllers.Single();
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
}