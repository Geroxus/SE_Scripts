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
    class LogTarget
    {
        public List<IMyTextSurface> TextSurfaces { get; } = new List<IMyTextSurface>();
        
        private readonly StringBuilder _builder = new StringBuilder();

        public LogTarget Log(string content, bool newLine = false)
        {
            _builder.Append(content);
            if (newLine) _builder.AppendLine();
            return this;
        }

        public LogTarget SetStyle(ContentType contentType = ContentType.TEXT_AND_IMAGE, string font = "Debug")
        {
            foreach (IMyTextSurface surface in TextSurfaces)
            {
                surface.ContentType = contentType;
                surface.Font = font;
            }
            return this;
        }

        public LogTarget Write()
        {
            foreach (IMyTextSurface surface in TextSurfaces) surface.WriteText(_builder);
            return this;
        }

        public LogTarget Consume(Action<StringBuilder> action)
        {
            action(_builder);
            return this;
        }

        public void Clear()
        {
            _builder.Clear();
        }
    }
    class Logger
    {
        private readonly IMyProgrammableBlock _me;
        private readonly IMyGridTerminalSystem _gridTerminalSystem;
        private readonly string _font;

        private readonly Dictionary<string, LogTarget> _logTargets = new Dictionary<string, LogTarget>();
        private string _defaultControlSurfaceName;

        public Logger(IMyProgrammableBlock me, IMyGridTerminalSystem terminalSystem, string font = "Green")
        {
            _me = me;
            _gridTerminalSystem = terminalSystem;
            _font = font;
        }

        public LogTarget To(string controlSurfaceName)
        {
            return _logTargets[controlSurfaceName];
        }

        public void CollectTextSurfaces(params string[] controlSurfaceNames)
        {
            if (controlSurfaceNames.Length == 0) throw new ArgumentException("No control surfaces defined.");

            _defaultControlSurfaceName = controlSurfaceNames[0];
            foreach (string controlSurfaceName in controlSurfaceNames)
            {
                _logTargets.Add(controlSurfaceName, new LogTarget());
            }

            _logTargets[_defaultControlSurfaceName].TextSurfaces.Add(_me.GetSurface(0));

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
                        _logTargets[controlSurfaceName].TextSurfaces.Add(textSurfaceProvider.GetSurface(index));
                    }
                }
            }
        }

        public Logger Log(string content, bool newLine = false, string controlSurfaceName = null)
        {
            if (_defaultControlSurfaceName == null) throw new ArgumentNullException(nameof(_defaultControlSurfaceName));
            if (controlSurfaceName == null) controlSurfaceName = _defaultControlSurfaceName;

            To(controlSurfaceName).Log(content, newLine);
            return this;
        }

        public void WriteOutput()
        {
            foreach (LogTarget target in _logTargets.Values)
            {
                target.SetStyle(font: _font)
                    .Write()
                    .Clear();
            }
        }
    }
}