﻿using Assets.Scripts.Network.HandlerBase;
using RebuildSharedData.Networking;
using UnityEngine;

namespace Assets.Scripts.Network.IncomingPacketHandlers.Environment
{
    [ClientPacketHandler(PacketType.EffectAtLocation)]
    public class PacketEffectAtLocation : ClientPacketHandlerBase
    {
        public override void ReceivePacket(ClientInboundMessage msg)
        {
            var effect = msg.ReadInt32();
            var pos = new Vector2Int(msg.ReadInt16(), msg.ReadInt16());
            var facing = msg.ReadInt32();

            var spawn = Camera.WalkProvider.GetWorldPositionForTile(pos);
            Camera.CreateEffect(effect, spawn, facing);
        }
    }
}