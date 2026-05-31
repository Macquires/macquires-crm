namespace Application.Common.Telecom;

/// <summary>Origin channel for <see cref="Domain.Entities.TelecomTechnicalTicket"/> creation.</summary>
public static class TechnicalTicketCreatedByChannel
{
    public const string CallCenterAgent = "CallCenter_Agent";
    public const string CustomerCareVoiceAi = "Customer_Care_Voice_AI";
    public const string ShowroomAgent = "Showroom_Agent";
    public const string SelfCareApp = "Self_Care_App";

    public const string VoiceAiSystemUserId = "system-voice-ai";
}
