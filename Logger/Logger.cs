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

namespace IngameScript
{
    class Logger
    {
        private readonly IMyProgrammableBlock _me;
        private readonly IMyGridTerminalSystem _gridTerminalSystem;
        private readonly List<IMyTextSurface> _textSurfaces = new List<IMyTextSurface>();
        private readonly string _font;
        private readonly StringBuilder _output = new StringBuilder();

        public Logger(IMyProgrammableBlock me, IMyGridTerminalSystem terminalSystem, string font = "Green")
        {
            _me = me;
            _gridTerminalSystem = terminalSystem;
            _font = font;
        }

        public void CollectTextSurfaces(string controlSurfaceName)
        {
            _textSurfaces.RemoveAll(_ => true);

            _textSurfaces.Add(_me.GetSurface(0));

            List<IMyTextSurfaceProvider> textSurfaceProviders = new List<IMyTextSurfaceProvider>();
            _gridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(textSurfaceProviders);
            foreach (IMyTextSurfaceProvider textSurfaceProvider in textSurfaceProviders)
            {
                var myTerminalBlock = textSurfaceProvider as IMyTerminalBlock;
                if (myTerminalBlock != null && myTerminalBlock.CustomData.Contains(controlSurfaceName))
                {
                    foreach (string dataLine in myTerminalBlock.CustomData.Split('\n'))
                    {
                        if (dataLine.StartsWith(controlSurfaceName))
                        {
                            int index = Convert.ToInt32(dataLine.Split(':')[1].Trim());
                            _textSurfaces.Add(textSurfaceProvider.GetSurface(index));
                        }
                    }
                }
            }
        }

        public Logger Log(string content, bool newLine = false)
        {
            _output.Append(content);
            if (newLine) _output.Append('\n');
            return this;
        }
        
        public void WriteOutput()
        {
            foreach (IMyTextSurface surface in _textSurfaces)
            {
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.Font = _font;
                surface.WriteText(_output.ToString());
            }

            _output.Clear();
        }

        public void WriteOutput(Action<string> action)
        {
            action(_output.ToString());
            WriteOutput();
        }
    }
}
