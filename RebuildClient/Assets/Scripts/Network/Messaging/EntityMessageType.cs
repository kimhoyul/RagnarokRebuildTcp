﻿namespace Assets.Scripts.Network.Messaging
{
    public enum EntityMessageType : byte
    {
        None,
        ShowDamage,
        ComboDamage,
        AttackMotion,
        TakeHit,
        HitEffect,
        Miss
    }
}