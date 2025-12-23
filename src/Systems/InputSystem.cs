using System;
using MoonTools.ECS;
using MoonWorks.Input;
using Tactician.Components;

namespace Tactician.Systems;

public struct InputState {
    public ButtonState Left { get; set; }
    public ButtonState Right { get; set; }
    public ButtonState Up { get; set; }
    public ButtonState Down { get; set; }
}

public class ControlSet {
    public VirtualButton Left { get; set; } = new EmptyButton();
    public VirtualButton Right { get; set; } = new EmptyButton();
    public VirtualButton Up { get; set; } = new EmptyButton();
    public VirtualButton Down { get; set; } = new EmptyButton();
}

public class InputSystem : MoonTools.ECS.System {
    private readonly ControlSet _playerOneGamepad = new();
    private readonly ControlSet _playerOneKeyboard = new();
    private readonly ControlSet _playerTwoGamepad = new();
    private readonly ControlSet _playerTwoKeyboard = new();

    public InputSystem(World world, Inputs inputs) : base(world) {
        Inputs = inputs;
        PlayerFilter = FilterBuilder.Include<Player>().Build();

        _playerOneKeyboard.Up = Inputs.Keyboard.Button(KeyCode.W);
        _playerOneKeyboard.Down = Inputs.Keyboard.Button(KeyCode.S);
        _playerOneKeyboard.Left = Inputs.Keyboard.Button(KeyCode.A);
        _playerOneKeyboard.Right = Inputs.Keyboard.Button(KeyCode.D);

        _playerOneGamepad.Up = Inputs.GetGamepad(0).LeftYDown;
        _playerOneGamepad.Down = Inputs.GetGamepad(0).LeftYUp;
        _playerOneGamepad.Left = Inputs.GetGamepad(0).LeftXLeft;
        _playerOneGamepad.Right = Inputs.GetGamepad(0).LeftXRight;

        _playerTwoKeyboard.Up = Inputs.Keyboard.Button(KeyCode.Up);
        _playerTwoKeyboard.Down = Inputs.Keyboard.Button(KeyCode.Down);
        _playerTwoKeyboard.Left = Inputs.Keyboard.Button(KeyCode.Left);
        _playerTwoKeyboard.Right = Inputs.Keyboard.Button(KeyCode.Right);

        _playerTwoGamepad.Up = Inputs.GetGamepad(1).LeftYDown;
        _playerTwoGamepad.Down = Inputs.GetGamepad(1).LeftYUp;
        _playerTwoGamepad.Left = Inputs.GetGamepad(1).LeftXLeft;
        _playerTwoGamepad.Right = Inputs.GetGamepad(1).LeftXRight;
    }

    private Inputs Inputs { get; }

    private Filter PlayerFilter { get; }

    public override void Update(TimeSpan timeSpan) {
        foreach (var playerEntity in PlayerFilter.Entities) {
            var index = Get<Player>(playerEntity).Index;
            var controlSet = index == 0 ? _playerOneKeyboard : _playerTwoKeyboard;
            var altControlSet = index == 0 ? _playerOneGamepad : _playerTwoGamepad;

            var inputState = InputState(controlSet, altControlSet);

            Set(playerEntity, inputState);
        }
    }

    private static InputState InputState(ControlSet controlSet, ControlSet altControlSet) {
        return new InputState {
            Left = controlSet.Left.State | altControlSet.Left.State,
            Right = controlSet.Right.State | altControlSet.Right.State,
            Up = controlSet.Up.State | altControlSet.Up.State,
            Down = controlSet.Down.State | altControlSet.Down.State,
        };
    }
}