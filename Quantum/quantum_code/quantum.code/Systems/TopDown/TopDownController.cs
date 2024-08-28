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
    
    public partial struct TopDownController
    {
        public static TopDownKCCSettings _settings = new TopDownKCCSettings();

        public void Init()
        {
            _settings.Init(ref this);
        }
        
        public void Move(Frame f, EntityRef entity, FPVector2 direction)
        {
            var movement = _settings.ComputeRawMovement(f, entity, direction, f.Context.TargetMapAndPlayersMask, QueryOptions.ComputeDetailedInfo | QueryOptions.HitAll);
            _settings.SteerAndMove(f, entity, movement);
        }
    }

    public unsafe partial class TopDownKCCSettings
    {
        // TODO: Move to config. @Nik 
        public readonly Int32 MaxContacts = 4; // fixed array in component too
        public readonly FP AllowedPenetration = FP._0_10;
        public readonly FP CorrectionSpeed = FP._10;
        public readonly FP BaseSpeed = FP._0_75;
        public readonly FP Acceleration = FP._10 * 8;
        public readonly Boolean Debug = false;
        public readonly FP Brake = FP._10 * 8;
        public readonly  Shape2D shape = Shape2D.CreateCircle(FP._0_20 + FP._0_20); 
        
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
        
        public TopDownKCCMovementData ComputeRawMovement(Frame f, EntityRef entity, FPVector2 direction, int layerMask = -1, QueryOptions queryOptions = QueryOptions.HitAll)
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
           
            var hits = f.Physics2D.OverlapShape(transform->Position, FP._0, shape, layerMask, options: queryOptions | QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo);
            int count = Math.Min(MaxContacts, hits.Count);

            HostProfiler.Start("Validate leaving feet collisions");
            var collisions = kcc->CollidingWith;

            //if(!f.Has<BotCharacter>(entity)) Log.Warn(entity+"Current Collisions: ",string.Join(",", collisions));
            
            for (var i = 0; i < collisions.Length; i++)
            {
                var c = collisions[i];
                
                if (!c.IsValid) continue;
                
                bool left = true;
                for (int j = 0; j < hits.Count && count > 0; j++)
                {
                    if (hits[j].Entity == c)
                    {
                        left = false;
                        break;
                    }
                }

                if (left)
                {
                    collisions[i] = EntityRef.None;
                    //if(!f.Has<BotCharacter>(entity)) Log.Warn("COL LEFT "+entity);
                    f.Signals.OnFeetCollisionLeft(entity, c);
                }
            }
            HostProfiler.End();
            bool newHit = false;
            
            HostProfiler.Start("Processing Hits");
            if (hits.Count > 0)
            {
                var initialized = false;
                hits.Sort(transform->Position);
                for (int i = 0; i < hits.Count && count > 0; i++)
                {
                    var hit = hits[i];
                    
                    if (hit.Entity == entity)
                    {
                        continue;
                    }
                    
                    if (hit.Entity.IsValid)
                    {
                        //if(!f.Has<BotCharacter>(entity)) Log.Warn("Feet Hit valid entity "+hit.Entity);
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
                            //if(!f.Has<BotCharacter>(entity)) Log.Warn("NEW HIT ! "+hit.Entity);
                            
                            f.Signals.OnFeetCollisionEnter(entity, hit.Entity, hit.Point);
                        }
                        else
                        {
                            f.Signals.OnFeetCollisionContinue(entity, hit.Entity);
                        }
                        HostProfiler.End();
                    }
                    
                    if (hit.IsTrigger)
                    {
                        continue;
                    }
                    
                    var contactPoint = hit.Point;
                    var contactToCenter = transform->Position - contactPoint;
                    var localDiff = contactToCenter.Magnitude - shape.Circle.Radius;
                    var localNormal = contactToCenter.Normalized;

                    var other = hits[i].Entity;

                    HostProfiler.Start("Processing 2D Body Contact");
                    if (other != default && f.Exists(other) == true && f.Has<TopDownController>(other) && f.TryGet<PhysicsCollider2D>(other, out var otherCollider))
                    {
                        var otherTransform = f.Get<Transform2D>(other);
                        var centerToCenter = otherTransform.Position - transform->Position;
                        var maxRadius = FPMath.Max(shape.Circle.Radius, otherCollider.Shape.Circle.Radius);
                        if (centerToCenter.Magnitude <= maxRadius)
                        {
                            localDiff = -maxRadius;
                            localNormal = entity.Index > other.Index ? FPVector2.Right : FPVector2.Left;
                        }
                    }
                    HostProfiler.End();
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