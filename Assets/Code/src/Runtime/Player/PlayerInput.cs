using UnityEngine;

namespace HouraiTeahouse.FantasyCrescendo {

/// <summary>
/// A data object representing the complete input of one player for a given
/// tick.
/// </summary>
public struct PlayerInput : IValidatable {

  // One Player Total: 17 bytes
  // Four Player Total: 68 bytes
  //
  // 60 times one: 1020 bytes
  // 60 times four: 4080 bytes

  public bool IsValid;                          // 1 bit

  public Vector2 Movement;                      // 8 bytes
  public Vector2 Smash;                         // 8 bytes

  public bool Attack;                           // 1 bit
  public bool Special;                          // 1 bit
  public bool Jump;                             // 1 bit
  public bool Shield;                           // 1 bit
  public bool Grab;                             // 1 bit

  bool IValidatable.IsValid => IsValid;

  public void Merge(PlayerInput other) {
    IsValid = IsValid || other.IsValid;
    Movement = MergeDirection(Movement, other.Movement);
    Smash = MergeDirection(Smash, other.Smash);
    Attack = Attack || other.Attack;
    Special = Special || other.Special;
    Jump = Jump || other.Jump;
    Shield = Shield || other.Shield;
    Grab = Grab|| other.Grab;
  }

  Vector2 MergeDirection(Vector2 a, Vector2 b) {
    return new Vector2(Mathf.Clamp(a.x + b.x, -1, 1), Mathf.Clamp(a.y + b.y, -1, 1));
  }

}

/// <summary>
/// A data object for managing the state and change of a single
/// player's input over two ticks of gameplay.
/// </summary>
public class PlayerInputContext : IValidatable {

  public PlayerInput Previous;
  public PlayerInput Current;

  public void Update(PlayerInput input) {
    Previous = Current;
    Current = input;
  }

  public bool IsValid => Previous.IsValid && Current.IsValid;

  public DirectionalInput Movement => Current.Movement;
  public DirectionalInput Smash => Current.Smash;

  public ButtonContext Attack {
    get {
      return new ButtonContext {
        Previous = Previous.Attack,
        Current  = Current.Attack
      };
    }
  }

  public ButtonContext Special {
    get {
      return new ButtonContext {
        Previous = Previous.Special,
        Current = Current.Special
      };
    }
  }

  public ButtonContext Jump {
    get {
      return new ButtonContext {
        Previous = Previous.Jump,
        Current = Current.Jump
      };
    }
  }

  public ButtonContext Shield {
    get {
      return new ButtonContext {
        Previous = Previous.Shield,
        Current = Current.Shield
      };
    }
  }

  public ButtonContext Grab {
    get {
      return new ButtonContext {
        Previous = Previous.Grab,
        Current = Current.Grab
      };
    }
  }

}

/// <summary>
/// A simple data object for managing the state and change of a single
/// button over two ticks of gameplay.
/// </summary>
public struct ButtonContext {

  public bool Previous;
  public bool Current;

  public bool WasPressed => !Previous && Current;
  public bool WasReleased => Previous && !Current;

}

public struct DirectionalInput {

  // TODO(james7132): Move this to a Config
  public const float DeadZone = 0.3f;

  public Vector2 Value;
  public Direction Direction => Direction.Neutral;

  public static implicit operator DirectionalInput(Vector2 dir) {
    return new DirectionalInput {
      Value = dir
    };
  }

}

public enum Direction {
  Neutral,
  Up,
  Down,
  Left,
  Right
}


}