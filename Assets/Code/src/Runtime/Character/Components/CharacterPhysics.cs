using HouraiTeahouse.FantasyCrescendo.Players;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace HouraiTeahouse.FantasyCrescendo.Characters {

[RequireComponent(typeof(CharacterStateMachine))]
public class CharacterPhysics : MonoBehaviour, IPlayerSimulation, IPlayerView {

  static Collider[] colliderDummy = new Collider[1];

  public CharacterController CharacterController;

  public float Gravity = 9.86f;

  public float MaxFallSpeed = 7.5f;
  public float FastFallSpeed = 10f;
  public Transform LedgeGrabBone;

  public bool IsGrounded { get; private set; }

  public Task Initialize(PlayerConfig config, bool isView) {
    if (LedgeGrabBone == null) {
      LedgeGrabBone = transform;
    }

    if (CharacterController == null) {
      CharacterController = GetComponent<CharacterController>();
    }

    var rigidbody = GetComponent<Rigidbody>();
    if (rigidbody == null) {
      rigidbody = gameObject.AddComponent<Rigidbody>();
    }
    rigidbody.useGravity = false;
    rigidbody.isKinematic = true;
    rigidbody.hideFlags = HideFlags.HideInInspector;

    return Task.CompletedTask;
  }

  public void Presimulate(ref PlayerState state) {
    ApplyState(ref state);
    IsGrounded = IsCharacterGrounded(state);
  }

  public void Simulate(ref PlayerState state, PlayerInputContext input) {
    ApplyGravity(ref state);
    LimitFallSpeed(ref state);

    Physics.SyncTransforms();
    // TODO(james7132): Move this somewhere more sane.
    Platform.CollisionStatusCheckAll(CharacterController);

    if (state.IsGrabbingLedge)  {
      SnapToLedge(ref state); 
      state.GrabbedLedgeTimer--;
      if (state.GrabbedLedgeTimer <= 0) {
        state.ReleaseLedge();
      }
    } else {
      CharacterController.Move(state.Velocity * Time.fixedDeltaTime);
      state.Position = transform.position;
      if (IsCharacterGrounded(state)) {
        state = SnapToGround(state);
      }
      if (state.GrabbedLedgeTimer < 0) {
        state.GrabbedLedgeTimer++;
      }
    }
  }

  public void ApplyState(ref PlayerState state) {
    transform.position = state.Position;
    var offset = 90f;
    if (!state.Direction) {
      offset += 180f;
    }
    transform.localEulerAngles = offset * Vector3.up;
  }

  void SnapToLedge(ref PlayerState state) {
    var targetLedge = Registry.Get<Ledge>().Get(state.GrabbedLedgeID);
    Assert.IsNotNull(targetLedge);
    var relativeDisplacement = LedgeGrabBone.position - transform.position;
    state.Position = targetLedge.transform.position - relativeDisplacement;
    state.Direction = targetLedge.Direction;
    state.IsFastFalling = false;
    state.JumpCount = 0;
  }

  public void ResetState(ref PlayerState state) {
    state.Velocity = Vector2.zero;
  }

  void ApplyGravity(ref PlayerState state) {
    if (state.IsRespawning || state.IsGrabbingLedge) return;
    if (!IsGrounded) {
      state.VelocityY -= Gravity * Time.fixedDeltaTime;
    } else {
      state.VelocityY = Mathf.Max(0f, state.Velocity.y);
    }
  }

  void LimitFallSpeed(ref PlayerState state) {
    if (IsGrounded) return;
    if (state.IsFastFalling) {
      state.VelocityY = -FastFallSpeed;
    } else if (state.Velocity.y < -MaxFallSpeed) {
      state.VelocityY = -MaxFallSpeed;
    }
  }

  PlayerState SnapToGround(PlayerState state) {
    var config = Config.Get<PhysicsConfig>();
    var pool = ArrayPool<RaycastHit>.Shared;
    var hits = pool.Rent(1);
    var offset = Vector3.up * CharacterController.height * 0.5f;
    var start = Vector3.up * config.GroundedSnapOffset;
    var top = transform.TransformPoint(CharacterController.center + offset) + start;
    var bottom = transform.TransformPoint(CharacterController.center - offset) + start;
    var distance = config.GroundedSnapOffset + config.GroundedSnapDistance;
    var count = Physics.CapsuleCastNonAlloc(top, bottom, CharacterController.radius, Vector3.down, hits, 
                                            distance, config.StageLayers, QueryTriggerInteraction.Ignore);
    if (count > 0) {
      // TODO(james7132): Properly transform this back.
      var pos = hits[0].point;
      if (Mathf.Approximately(pos.x, state.Position.x)) {
        state.Position = hits[0].point;
      }
    }
    pool.Return(hits);
    return state;
  }

  bool IsCharacterGrounded(PlayerState state) {
    if (state.VelocityY > 0) return false;
    if (state.RespawnTimeRemaining > 0) return true;
    var center = Vector3.zero;
    var radius = 1f;
    if (CharacterController != null) {
      // TODO(james7132): Remove these magic numbers
      center = CharacterController.center - Vector3.up * (CharacterController.height * 0.50f - CharacterController.radius * 0.5f);
      radius = CharacterController.radius * 0.75f;
    }

    var stageLayers = Config.Get<PhysicsConfig>().StageLayers;
    center = transform.TransformPoint(center);

    var count = Physics.OverlapSphereNonAlloc(center, radius, colliderDummy, stageLayers, QueryTriggerInteraction.Ignore);
    return count != 0;
  }

}

}
