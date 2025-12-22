namespace Scv.Api.Models.auth
{
    public interface IFileAccessRequest
    {
        string UserId { get; }
        string UserName { get; }
        string FileId { get; }
        string AgencyId { get; }
        string PartId { get; }
    }
}
