namespace Internal
{
    public enum MessageTypes
    {
        JOIN_SESSION,
        JOINED_SUCCESSFULLY,
        PEER_JOINED,
        LEAVE_SESSION,
        PEER_LEFT,
        SDP_OFFER,
        SDP_ANWSER,
        CANDIDATE,
        NO_SESSION,
        OPEN_DATA_CHANNEL,
        CLOSE_DATA_CHANNEL,
        MUTE_DATA_CHANNEL,
        UNMUTE_DATA_CHANNEL,
        KICK_TARGET,
        CHANGE_READ_PERMISSION,
        CHANGE_WRITE_PERMISSION,
    }
}