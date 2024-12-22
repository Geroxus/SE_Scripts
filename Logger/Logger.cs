using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using VRageRender;

namespace IngameScript
{
    class Logger
    {
        private readonly IMyProgrammableBlock _me;
        private readonly IMyGridTerminalSystem _gridTerminalSystem;
        private readonly string _font;

        private readonly Dictionary<string, List<IMyTextSurface>> _textSurfaces =
            new Dictionary<string, List<IMyTextSurface>>();

        private readonly Dictionary<string, StringBuilder> _outputs = new Dictionary<string, StringBuilder>();
        private string _defaultControlSurfaceName;

        public Logger(IMyProgrammableBlock me, IMyGridTerminalSystem terminalSystem, string font = "Green")
        {
            _me = me;
            _gridTerminalSystem = terminalSystem;
            _font = font;
        }

        public void CollectTextSurfaces(params string[] controlSurfaceNames)
        {
            if (controlSurfaceNames.Length == 0) throw new ArgumentException("No control surfaces defined.");

            _textSurfaces.Clear();

            _defaultControlSurfaceName = controlSurfaceNames[0];
            foreach (string controlSurfaceName in controlSurfaceNames)
            {
                _textSurfaces[controlSurfaceName] = new List<IMyTextSurface>();
                _outputs.Add(controlSurfaceName, new StringBuilder());
            }

            _textSurfaces[_defaultControlSurfaceName].Add(_me.GetSurface(0));

            List<IMyTextSurfaceProvider> textSurfaceProviders = new List<IMyTextSurfaceProvider>();
            _gridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(textSurfaceProviders);
            foreach (IMyTextSurfaceProvider textSurfaceProvider in textSurfaceProviders)
            {
                foreach (string controlSurfaceName in controlSurfaceNames)
                {
                    var myTerminalBlock = textSurfaceProvider as IMyTerminalBlock;
                    if (myTerminalBlock == null || !myTerminalBlock.CustomData.Contains(controlSurfaceName)) continue;
                    foreach (string dataLine in myTerminalBlock.CustomData.Split('\n'))
                    {
                        if (!dataLine.StartsWith(controlSurfaceName)) continue;
                        int index = Convert.ToInt32(dataLine.Split(':')[1].Trim());
                        _textSurfaces[controlSurfaceName].Add(textSurfaceProvider.GetSurface(index));
                    }
                }
            }
        }

        public Logger Log(string content, bool newLine = false, string controlSurfaceName = null)
        {
            if (_defaultControlSurfaceName == null) throw new ArgumentNullException(nameof(_defaultControlSurfaceName));
            if (controlSurfaceName == null) controlSurfaceName = _defaultControlSurfaceName;

            _outputs[controlSurfaceName].Append(content);
            if (newLine) _outputs[controlSurfaceName].Append('\n');
            return this;
        }

        public void WriteOutput()
        {
            foreach (string controlSurfaceName in _outputs.Keys)
            {
                foreach (IMyTextSurface surface in _textSurfaces[controlSurfaceName])
                {
                    surface.ContentType = ContentType.TEXT_AND_IMAGE;
                    surface.Font = _font;
                    surface.WriteText(_outputs[controlSurfaceName].ToString());
                }

                _outputs[controlSurfaceName].Clear();
            }
        }
    }
}