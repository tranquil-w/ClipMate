namespace ClipMate.Service.Interfaces
{
    public interface IAdminService
    {
        bool IsRunningAsAdministrator();
        bool RestartAsAdministrator();
    }
}
