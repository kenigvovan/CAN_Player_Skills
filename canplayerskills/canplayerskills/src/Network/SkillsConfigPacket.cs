using ProtoBuf;

namespace canplayerskills.Network
{
    /// <summary>Effective config sent to clients on join, as json.</summary>
    [ProtoContract]
    public class SkillsConfigPacket
    {
        public const string ChannelName = "canplayerskills";

        [ProtoMember(1)]
        public string Json = "";
    }
}
