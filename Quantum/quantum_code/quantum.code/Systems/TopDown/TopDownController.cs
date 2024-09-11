using System;
using Photon.Deterministic;
using Quantum.Core;
using Quantum.Profiling;

namespace Quantum
{
    public enum TopDownKCCMovementType
    {
        None,
        FromDirection,
        Tangent
    }

    public struct TopDownKCCMovementData
    {
        public TopDownKCCMovementType Type;
        public FPVector2 Correction;
        public FPVector2 Direction;
        public FP MaxPenetration;
    }
    
    /// <summary>
    /// This controls player movement.
    /// We have 2 main colliders on players. The feet collider, which is a shapecast you will find in this file,
    /// which is a trigger as its a shapecast, and a player hitbox collider, which is a entity collider and that's the
    /// main collider we use to see if something touched the player to damage
    /// </summary>
    public partial struct TopDownController
    {
        public static TopDownKCCSettings _settings = new TopDownKCCSettings();

        public void Init()
        {
            _settings.Init(ref this);
        }
        
        public void Move(Frame f, EntityRef entity, FPVector2 direction)
        {
            // TODO: Optimize by removing detailed info on collisions
            var isBot = f.Has<BotCharacter>(entity);
            var layer = isBot ? f.Context.TargetMapAndPlayersMask : f.Context.TargetMapAndPlayersMask;
            
            // TODO: We don't need all this masks, we should optimize collectibles and bushes to be kinematics
            var query = QueryOptions.HitStatics | QueryOptions.HitKinematics | QueryOptions.HitTriggers;
            if (!isBot || !QuantumFeatureFlags.BOTS_PHYSICS_IGNORE_OBSTACLES)
            {
                query |= QueryOptions.ComputeDetailedInfo;
            }
            var movement = _settings.ComputeRawMovement(f, entity, direction, layer, query);
            _settings.SteerAndMove(f, entity, movement);
        }
    }

    public unsafe partial class TopDownKCCSettings
    {
        // TODO: Move to config. @Nik 
        public readonly Int32 MaxContacts = 4; // fixed array in component too
        public readonly FP AllowedPenetration = FP._0_05;
        public readonly FP CorrectionSpeed = FP._1;
        public readonly FP BaseSpeed = FP._0_75;
        public readonly FP Acceleration = FP._10 * 8;
        public readonly Boolean Debug = false;
        public readonly FP Brake = FP._10 * 8;
        public readonly Shape2D PlayerFeetShape = Shape2D.CreateCircle(FP._0_20 + FP._0_20 + FP._0_10); 
        
        public void Init(ref TopDownController kcc)
        {
            kcc.Settings = this;
            kcc.MaxSpeed = BaseSpeed;
        }

        public void SteerAndMove(Frame f, EntityRef entity, in TopDownKCCMovementData movementData)
        {
            TopDownController* kcc = null;
            if (f.Unsafe.TryGetPointer(entity, out kcc) == false)
            {
                return;
            }

            Transform2D* transform = null;
            if (f.Unsafe.TryGetPointer(entity, out transform) == false)
            {
                return;
            }
            HostProfiler.Start("Calculating Steering");
            if (movementData.Type != TopDownKCCMovementType.None)
            {
                kcc->Velocity += Acceleration * f.DeltaTime * movementData.Direction;
                if (kcc->Velocity.SqrMagnitude > kcc->MaxSpeed * kcc->MaxSpeed)
                {
                    kcc->Velocity = kcc->Velocity.Normalized * kcc->MaxSpeed;
                }
            }
            else
            {
                kcc->Velocity = FPVector2.MoveTowards(kcc->Velocity, FPVector2.Zero, f.DeltaTime * Brake);
            }
            if (movementData.MaxPenetration > AllowedPenetration)
            {
                if (movementData.MaxPenetration > AllowedPenetration * 2)
                {
                    transform->Position += movementData.Correction;
                }
                else
                {
                    transform->Position += movementData.Correction * f.DeltaTime * CorrectionSpeed;
                }
            }
            transform->Position += kcc->Velocity * f.DeltaTime;
            HostProfiler.End();
        }
        
        public TopDownKCCMovementData ComputeRawMovement(Frame f, EntityRef entity, FPVector2 direction, int layerMask, QueryOptions queryOptions)
        {
            TopDownController* kcc = null;
            if (f.Unsafe.TryGetPointer(entity, out kcc) == false)
            {
                return default;
            }
            Transform2D* transform = null;
            if (f.Exists(entity) == false || f.Unsafe.TryGetPointer(entity, out transform) == false)
            {
                return default;
            }
            HostProfiler.Start("ComputeRawMovement");
            TopDownKCCMovementData movementPack = default;
            movementPack.Type = direction != default ? TopDownKCCMovementType.FromDirection : TopDownKCCMovementType.None;
            movementPack.Direction = direction;
            
            HostProfiler.Start("Feet Overlap Shape");
            // TODO: Convert to multi-threaded query for performance 
            var feetCollision = f.Physics2D.OverlapShape(transform->Position, FP._0, PlayerFeetShape, layerMask: layerMask, options: queryOptions);
            HostProfiler.End();
            
            int count = Math.Min(MaxContacts, feetCollision.Count);

            HostProfiler.Start("Validate leaving feet collisions");
            var collisions = kcc->CollidingWith;
            
            for (var i = 0; i < collisions.Length; i++)
            {
                var c = collisions[i];
                
                if (!c.IsValid) continue;
                
                bool left = true;
                for (int j = 0; j < feetCollision.Count && count > 0; j++)
                {
                    if (feetCollision[j].Entity == c)
                    {
                        left = false;
                        break;
                    }
                }

                if (left)
                {
                    collisions[i] = EntityRef.None;
                    f.Signals.OnFeetCollisionLeft(entity, c);
                }
            }
            HostProfiler.End();
            bool newHit = false;
            
            HostProfiler.Start("Processing Hits");
            if (feetCollision.Count > 0)
            {
                var initialized = false;
                feetCollision.Sort(transform->Position);
                for (int i = 0; i < feetCollision.Count && count > 0; i++)
                {
                    var hit = feetCollision[i];
                    
                    if (hit.Entity == entity)
                    {
                        continue;
                    }
                    
                    if (hit.Entity.IsValid)
                    {
                        newHit = true;
                        var openIndex = -1;
                        for (var j = 0; j < collisions.Length; j++)
                        {
                            if (collisions[j] == hit.Entity)
                            {
                                newHit = false;
                                break;
                            }
                            if (!collisions[j].IsValid) openIndex = j;
                        }
                        HostProfiler.Start("Processing Feet Collision Signal");
                        if (newHit && openIndex != -1)
                        {
                            collisions[openIndex] = hit.Entity;
                            f.Signals.OnFeetCollisionEnter(entity, hit.Entity, hit.Point);
                        }
                        else
                        {
                            f.Signals.OnFeetCollisionContinue(entity, hit.Entity);
                        }
                        HostProfiler.End();
                    }
         
                    // Bots wont apply movement correction, we just trust the ai agent
                    // this way we can avoid odd stucks or events
                    // this means a bot can walk anywhere it's ai thinks it can basically
                    if (QuantumFeatureFlags.BOTS_PHYSICS_IGNORE_OBSTACLES && f.Has<BotCharacter>(entity))
                    {
                        continue;
                    }
                    
                    var other = feetCollision[i].Entity;
                    if (hit.IsTrigger)
                    {
                        continue;
                    }
                    
                
                    var contactPoint = hit.Point;
                    var contactToCenter = transform->Position - contactPoint;
                    var localDiff = contactToCenter.Magnitude - PlayerFeetShape.Circle.Radius;
                    var localNormal = contactToCenter.Normalized;
                    
                    if (QuantumFeatureFlags.PLAYER_PUSHING && other != default && f.Exists(other) == true && f.Has<TopDownController>(other) && f.TryGet<PhysicsCollider2D>(other, out var otherCollider))
                    {
                        HostProfiler.Start("Processing 2D Body Contact");
                        var otherTransform = f.Get<Transform2D>(other);
                        var centerToCenter = otherTransform.Position - transform->Position;
                        var maxRadius = FPMath.Max(PlayerFeetShape.Circle.Radius, otherCollider.Shape.Circle.Radius);
                        if (centerToCenter.Magnitude <= maxRadius)
                        {
                            localDiff = -maxRadius;
                            localNormal = entity.Index > other.Index ? FPVector2.Right : FPVector2.Left;
                        }
                        HostProfiler.End();
                    }
                  
                    count--;
                    if (!initialized)
                    {
                        HostProfiler.Start("Initialization of Math");
                        initialized = true;
                        if (direction != default)
                        {
                            var angle = FPVector2.RadiansSkipNormalize(direction.Normalized, localNormal);
                            if (angle >= FP.Rad_90)
                            {
                                var d = FPVector2.Dot(direction, localNormal);
                                var tangentVelocity = direction - localNormal * d;
                                if (tangentVelocity.SqrMagnitude > FP.EN4)
                                {
                                    movementPack.Direction = tangentVelocity.Normalized;
                                    movementPack.Type = TopDownKCCMovementType.Tangent;
                                }
                                else
                                {
                                    movementPack.Direction = default;
                                    movementPack.Type = TopDownKCCMovementType.None;
                                }

                            }
                        }
                        HostProfiler.End();
                        movementPack.MaxPenetration = FPMath.Abs(localDiff);
                    }
                    var localCorrection = localNormal * -localDiff;
                    movementPack.Correction += localCorrection;
                }
            } 
            HostProfiler.End();
            HostProfiler.End();
            return movementPack;
        }
    }
}