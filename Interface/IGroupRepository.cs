using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IGroupRepository
    {
        Task<List<GroupDto>> GetGroupsAsync();
        Task<List<SubGroupDto>> GetSubGroupsAsync();
        Task<bool> GroupNameExistsAsync(string groupName);
        Task<bool> GroupExistsAsync(long portalId);
        Task<bool> SubGroupNameExistsAsync(long portalId, string subGroupName);
        Task<GroupDto?> CreateGroupAsync(CreateGroupRequest request);
        Task<SubGroupDto?> CreateSubGroupAsync(CreateSubGroupRequest request);
    }
}
