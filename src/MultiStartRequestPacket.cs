using System.Collections.Generic;
using UnityEngine;

namespace MoreMultiPlayer
{
    public struct MultiStartRequestPacket
    {
        public int seqNum;
        public int seed;
        public byte nrOfPlayers;
        public byte nrOfAbilites;
        public byte currentLevel;
        public byte frameBufferSize;
        public ulong isDemoMask;
        public ulong[] p_ids;
        public byte[] p_colors;
        public byte[] p_teams;
        public byte[] p_ability1s;
        public byte[] p_ability2s;
        public byte[] p_ability3s;
    }
}
