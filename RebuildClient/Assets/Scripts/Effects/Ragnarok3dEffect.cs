﻿using System.Collections.Generic;
using Assets.Scripts.Network;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;

namespace Assets.Scripts.Effects
{
    public class Ragnarok3dEffect : MonoBehaviour
    {
        public float Duration;
        public int DurationFrames;
        public float CurrentPos;
        public int Step = -1;
        public int LastStep = 0;
        public int ObjCount = 0;
        public EffectType EffectType;

        public Vector3 PositionOffset = Vector3.zero;
        public ServerControllable SourceEntity;
        public GameObject FollowTarget;
        public GameObject AimTarget;
        public Object EffectData;

        public bool DestroyOnTargetLost = false;
        public bool UpdateOnlyOnFrameChange = false;

        public float ActiveDelay = 0f;
        public float PauseTime = 0f;

        public IEffectHandler EffectHandler;
        public IEffectOwner EffectOwner;
        public readonly List<RagnarokPrimitive> Primitives = new();
        public readonly List<GameObject> AttachedObjects = new();
        
        public bool IsInitialized = false;

        public int SourceEntityId => SourceEntity != null ? SourceEntity.Id : -1; 

        public List<RagnarokPrimitive> GetPrimitives => Primitives;

        public static Ragnarok3dEffect Create()
        {
            var go = new GameObject("Effect");
            var effect = go.AddComponent<Ragnarok3dEffect>();
            return effect;
        }

        public void SetEffectType(EffectType type)
        {
            EffectHandler = RagnarokEffectData.GetEffectHandler(type);
            EffectType = type;
            IsInitialized = true;
        }

        public void SetDurationByTime(float time)
        {
            Duration = time;
            DurationFrames = Mathf.FloorToInt(time * 60f);
        }
        
        public void SetDurationByFrames(int frame)
        {
            Duration = (frame + 1) * (1f / 60f);
            DurationFrames = frame;
        }

        public void AttachChildObject(GameObject obj)
        {
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;
            AttachedObjects.Add(obj);
        }

        public void Reset()
        {
            // Debug.Log("EffectReset");
            IsInitialized = false;
            FollowTarget = null;
            DestroyOnTargetLost = false;
            ActiveDelay = 0;
            PauseTime = 0;
            EffectHandler = null;
            EffectOwner = null;
            EffectData = null;
            AimTarget = null;
            Duration = 0;
            CurrentPos = 0;
            Step = -1;
            LastStep = -1;
            PositionOffset = Vector3.zero;
            EffectType = 0;
            
            for (var i = 0; i < Primitives.Count; i++)
            {
                var p = Primitives[i];
                RagnarokEffectPool.ReturnPrimitive(p);
                Primitives[i] = null;
            }

            for (var i = 0; i < AttachedObjects.Count; i++)
            {
                var obj = AttachedObjects[i];
                GameObject.Destroy(obj);
            }

            Primitives.Clear();
        }

        public RagnarokPrimitive LaunchPrimitive(PrimitiveType type, Material mat, float duration = -1)
        {
            var primitive = RagnarokEffectPool.GetPrimitive(this);

            var pHandler = RagnarokEffectData.GetPrimitiveHandler(type);
            if (pHandler != null)
                primitive.PrimitiveHandler = pHandler;

            primitive.Prepare(this, type, mat, duration);
            
            Primitives.Add(primitive);
            
            return primitive;
        }

        private void RenderDirtyPrimitives()
        {
            for (var i = 0; i < Primitives.Count; i++)
            {
                var p = Primitives[i];
                if (p.IsDirty)
                    p.RenderPrimitive();
            }
        }

        public void EndEffect()
        {
            EffectHandler.OnCleanup(this);
            RagnarokEffectPool.Return3dEffect(this);
        }

        private void EndEffectWithNotifyOwner()
        {
            EffectHandler.OnCleanup(this);
            if(EffectOwner != null)
                EffectOwner.OnEffectEnd(this); //notify owner to remove us, we've run our course
            RagnarokEffectPool.Return3dEffect(this);
        }

        public void Update()
        {
            ActiveDelay -= Time.deltaTime;
            if (ActiveDelay > 0f)
                return;

            if (PauseTime > 0f)
            {
                PauseTime -= Time.deltaTime;
                if (PauseTime > 0f)
                    return;
            }

            CurrentPos += Time.deltaTime;
            LastStep = Step;
            if (Mathf.RoundToInt(CurrentPos / (1 / 60f)) > Step)
                Step++; //only advance once per frame.
                
            if (FollowTarget == null && DestroyOnTargetLost)
            {
                EndEffectWithNotifyOwner();
                return;
            }

            if (FollowTarget != null)
                transform.localPosition = FollowTarget.transform.position + PositionOffset;

            var active = false;
            if (UpdateOnlyOnFrameChange)
            {
                if (LastStep != Step)
                    active = EffectHandler.Update(this, CurrentPos, Step);
                else
                    active = true;
            }
            else
                active = EffectHandler.Update(this, CurrentPos, Step);
            var anyActive = active;

            for (var i = 0; i < Primitives.Count; i++)
            {
                var p = Primitives[i];

                if (!p.IsActive)
                    continue;

                var pActive = p.UpdatePrimitive();
                if (pActive)
                    anyActive = true;

                if (pActive && p.IsDirty)
                    p.RenderPrimitive();
            }

            if (!anyActive)
                EndEffectWithNotifyOwner();
        }
    }
}