using System;
using System.IO;

namespace MoreMultiPlayer
{
    public static class NetworkToolsExtensions
    {
        public static int GetMultiStartRequestSize(int nrOfPlayers)
        {
            return 11 + nrOfPlayers * 13;
        }

        public static MultiStartRequestPacket ReadMultiStartRequest(BinaryReader reader)
        {
            MultiStartRequestPacket packet = new MultiStartRequestPacket();
            packet.seqNum = reader.ReadInt32();
            packet.seed = reader.ReadInt32();
            packet.nrOfPlayers = reader.ReadByte();
            packet.nrOfAbilites = reader.ReadByte();
            packet.currentLevel = reader.ReadByte();
            packet.frameBufferSize = reader.ReadByte();
            packet.isDemoMask = reader.ReadUInt64();

            packet.p_ids = new ulong[packet.nrOfPlayers];
            packet.p_colors = new byte[packet.nrOfPlayers];
            packet.p_teams = new byte[packet.nrOfPlayers];
            packet.p_ability1s = new byte[packet.nrOfPlayers];
            packet.p_ability2s = new byte[packet.nrOfPlayers];
            packet.p_ability3s = new byte[packet.nrOfPlayers];

            for (int i = 0; i < packet.nrOfPlayers; i++)
            {
                packet.p_ids[i] = reader.ReadUInt64();
                packet.p_colors[i] = reader.ReadByte();
                packet.p_teams[i] = reader.ReadByte();
                packet.p_ability1s[i] = reader.ReadByte();
                packet.p_ability2s[i] = reader.ReadByte();
                packet.p_ability3s[i] = reader.ReadByte();
            }

            return packet;
        }

        public static byte[] EncodeMultiStartRequest(MultiStartRequestPacket packet)
        {
            int size = GetMultiStartRequestSize(packet.nrOfPlayers);
            byte[] buffer = new byte[size];
            using (MemoryStream ms = new MemoryStream(buffer))
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(packet.seqNum);
                writer.Write(packet.seed);
                writer.Write(packet.nrOfPlayers);
                writer.Write(packet.nrOfAbilites);
                writer.Write(packet.currentLevel);
                writer.Write(packet.frameBufferSize);
                writer.Write(packet.isDemoMask);

                for (int i = 0; i < packet.nrOfPlayers; i++)
                {
                    writer.Write(packet.p_ids[i]);
                    writer.Write(packet.p_colors[i]);
                    writer.Write(packet.p_teams[i]);
                    writer.Write(packet.p_ability1s[i]);
                    writer.Write(packet.p_ability2s[i]);
                    writer.Write(packet.p_ability3s[i]);
                }
            }
            return buffer;
        }
    }
}
