﻿using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Network.Messaging
{
    public class EntityMessageQueue
    {
        public ServerControllable Owner;
        private readonly List<EntityMessage> messages = new();
        private bool isDirty;

        public bool HasMessages => messages.Count > 0;

        public bool TryGetMessage(out EntityMessage msg)
        {
            msg = null;
            if (messages.Count == 0)
                return false;
            if (isDirty)
            {
                messages.Sort(Compare);
                isDirty = false;
            }

            if (messages[0].ActivationTime < Time.timeSinceLevelLoad)
            {
                msg = messages[0];
                messages.RemoveAt(0);
                return true;
            }

            return false;   
        }

        public void EnqueueMessage(EntityMessage msg)
        {
            if (msg.ActivationTime < Time.timeSinceLevelLoad)
            {
                Owner.ExecuteMessage(msg);
                return;
            }
            
            messages.Add(msg);
            isDirty = true;
        }

        public float TimeUntilMessageLogClears(EntityMessageType type)
        {
            var min = 0f;
            for(var i = 0; i < messages.Count; i++)
                if (messages[i].Type == type && messages[i].ActivationTime > Time.timeSinceLevelLoad)
                    min = messages[i].ActivationTime;

            if (min > 0)
                return min - Time.timeSinceLevelLoad;
            
            return 0f;
        }

        private int Compare(EntityMessage left, EntityMessage right) => left.ActivationTime.CompareTo(right.ActivationTime);
        
        public void SendHitEffect(ServerControllable src, float time, int hitType = 1)
        {
            var msg = EntityMessagePool.Borrow();
            msg.ActivationTime = Time.timeSinceLevelLoad + time;
            msg.Type = EntityMessageType.HitEffect;
            msg.Entity = src; //the hit will come from this entity's position
            msg.Value1 = hitType;

            EnqueueMessage(msg);
        }
        
        public void SendMissEffect(float time)
        {
            var msg = EntityMessagePool.Borrow();
            msg.ActivationTime = Time.timeSinceLevelLoad + time;
            msg.Type = EntityMessageType.Miss;

            EnqueueMessage(msg);
        }

        public void SendDamageEvent(ServerControllable src, float time, int damage, int hitCount)
        {
            for (var i = 0; i < hitCount; i++)
            {
                var msg = EntityMessagePool.Borrow();
                msg.ActivationTime = Time.timeSinceLevelLoad + time + 0.2f * i;
                msg.Type = EntityMessageType.ShowDamage;
                msg.Entity = src;
                msg.Value1 = damage;
                if(hitCount > 1)
                    msg.Value2 = (i + 1) * damage;

                EnqueueMessage(msg);
            }
        }
    }
}