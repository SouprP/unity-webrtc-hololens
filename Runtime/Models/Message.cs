using System;

namespace Models
{
    [Serializable]
    public class Message
    {
        public string type;

        // Factory method to create Message objects based on type
        public static Message CreateMessage(string type)
        {
            return type switch
            {
                "join_session" => new JoinSessionMessage(),
                "joined_successfully" => new JoinedSuccessfullyMessage(),
                "peer_joined" => new PeerJoinedMessage(),
                "leave_session" => new LeaveSessionMessage(),
                "peer_left" => new PeerLeftMessage(),
                "sdp_offer" => new SdpOfferMessage(),
                "sdp_answer" => new SdpAnswerMessage(),
                "candidate" => new CandidateMessage(),
                "no_session" => new NoSessionMessage(),
                "open_data_channel" => new OpenDataChannelMessage(),
                "close_data_channel" => new CloseDataChannelMessage(),
                "mute_data_channel" => new MuteDataChannelMessage(),
                "unmute_data_channel" => new UnmuteDataChannelMessage(),
                "kick_target" => new KickTargetMessage(),
                "change_read_permission" => new ChangeReadPermissionMessage(),
                "change_write_permission" => new ChangeWritePermissionMessage(),
                _ => throw new ArgumentException("Unknown message type")
            };
        }
    }
    
    [Serializable]
    public class JoinSessionMessage : Message
    {
        public string session_id;
    }

    [Serializable]
    public class JoinedSuccessfullyMessage : Message
    {
        public int peer_amount;
    }

    [Serializable]
    public class PeerJoinedMessage : Message
    {
        public string peer_id;
    }

    [Serializable]
    public class LeaveSessionMessage : Message
    {
        public string session_id;
    }

    [Serializable]
    public class PeerLeftMessage : Message
    {
        public string peer_id;
    }

    [Serializable]
    public class SdpOfferMessage : Message
    {
        public string peer_id;
        public string sdp;
    }

    [Serializable]
    public class SdpAnswerMessage : Message
    {
        public string peer_id;
        public string sdp;
    }

    [Serializable]
    public class CandidateMessage : Message
    {
        public string peer_id;
        public string candidate;
    }

    [Serializable]
    public class NoSessionMessage : Message
    {
        // No additional properties needed for this type
    }

    [Serializable]
    public class OpenDataChannelMessage : Message
    {
        public string sdp;
    }

    [Serializable]
    public class CloseDataChannelMessage : Message
    {
        public string sdp;
    }

    [Serializable]
    public class MuteDataChannelMessage : Message
    {
        public string session_id;
        public string peer_id;
        public string target_id;
        public string channel_id;
    }

    [Serializable]
    public class UnmuteDataChannelMessage : Message
    {
        public string session_id;
        public string peer_id;
        public string target_id;
        public string channel_id;
    }

    [Serializable]
    public class KickTargetMessage : Message
    {
        public string session_id;
        public string peer_id;
        public string target_id;
    }

    [Serializable]
    public class ChangeReadPermissionMessage : Message
    {
        public string session_id;
        public string peer_id;
        public string target_id;
        public string channel_id;
    }

    [Serializable]
    public class ChangeWritePermissionMessage : Message
    {
        public string session_id;
        public string peer_id;
        public string target_id;
        public string channel_id;
    }
}