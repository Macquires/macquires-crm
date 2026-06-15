namespace Application.Common.Telecom.OperationCreate;

[Flags]
public enum OperationCreatePostCreateFlags
{
    None = 0,
    EnqueueTechnicalTicket = 1,
    ReserveDeviceInventory = 2,
}
