using System;
using System.Collections.Generic;
using Content.Client.Interfaces;
using Content.Shared;
using SS14.Client;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Player;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace Content.Client
{
    public class ClientNotifyManager : SharedNotifyManager, IClientNotifyManager
    {
#pragma warning disable 649
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IUserInterfaceManager _userInterfaceManager;
        [Dependency] private IInputManager _inputManager;
        [Dependency] private IEyeManager _eyeManager;
        [Dependency] private IClientNetManager _netManager;
#pragma warning restore 649

        private readonly List<PopupLabel> _aliveLabels = new List<PopupLabel>();
        private bool _initialized;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _netManager.RegisterNetMessage<MsgDoNotify>(nameof(MsgDoNotify), DoNotifyMessage);

            _initialized = true;
        }

        private void DoNotifyMessage(MsgDoNotify message)
        {
            PopupMessage(_eyeManager.WorldToScreen(message.Coordinates), message.Message);
        }

        public override void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message)
        {
            if (viewer != _playerManager.LocalPlayer.ControlledEntity)
            {
                return;
            }

            PopupMessage(_eyeManager.WorldToScreen(coordinates), message);
        }

        public void PopupMessage(ScreenCoordinates coordinates, string message)
        {
            var label = new PopupLabel {Text = message};
            var minimumSize = label.CombinedMinimumSize;
            label.InitialPos = label.Position = coordinates.Position - minimumSize / 2;
            _userInterfaceManager.StateRoot.AddChild(label);
            _aliveLabels.Add(label);
        }

        public void PopupMessage(string message)
        {
            PopupMessage(new ScreenCoordinates(_inputManager.MouseScreenPosition), message);
        }

        public void FrameUpdate(RenderFrameEventArgs eventArgs)
        {
            foreach (var label in _aliveLabels)
            {
                label.Update(eventArgs);
            }

            _aliveLabels.RemoveAll(l => l.Disposed);
        }

        private class PopupLabel : Label
        {
            private float _timeLeft;
            public Vector2 InitialPos { get; set; }

            protected override void Initialize()
            {
                base.Initialize();

                ShadowOffsetXOverride = 1;
                ShadowOffsetYOverride = 1;
                FontColorShadowOverride = Color.Black;

            }

            public void Update(RenderFrameEventArgs eventArgs)
            {
                _timeLeft += eventArgs.Elapsed;
                Position = InitialPos - new Vector2(0, 20 * (_timeLeft * _timeLeft + _timeLeft));
                if (_timeLeft > 0.5f)
                {
                    Modulate = Color.White.WithAlpha(1f - 0.2f * (float)Math.Pow(_timeLeft - 0.5f, 3f));
                    if (_timeLeft > 3f)
                    {
                        Dispose();
                    }
                }
            }
        }
    }

    public class PopupMessageCommand : IConsoleCommand
    {
        public string Command => "popupmsg";
        public string Description => "";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var arg = args[0];
            var mgr = IoCManager.Resolve<IClientNotifyManager>();
            mgr.PopupMessage(arg);
            return false;
        }
    }
}
