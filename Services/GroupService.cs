using SankalpAPI.Interface;
using SankalpAPI.Model;

namespace SankalpAPI.Services
{
    public class GroupService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger<GroupService> _logger;

        public GroupService(IGroupRepository groupRepository, ILogger<GroupService> logger)
        {
            _groupRepository = groupRepository;
            _logger = logger;
        }

        public async Task<GroupManagementResponse<GroupManagementListDto>> GetGroupManagementAsync()
        {
            try
            {
                var groups = await _groupRepository.GetGroupsAsync();
                var subGroups = await _groupRepository.GetSubGroupsAsync();

                return Success("Groups and subgroups retrieved successfully.", new GroupManagementListDto
                {
                    Groups = groups,
                    SubGroups = subGroups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to retrieve group management data.");
                return Failure<GroupManagementListDto>("Unable to retrieve groups and subgroups.");
            }
        }

        public async Task<GroupManagementResponse<GroupDto>> CreateGroupAsync(CreateGroupRequest request)
        {
            try
            {
                request.PortalName = request.PortalName.Trim();
                request.PortalUrl = CleanOptional(request.PortalUrl);
                request.PortalImagePath = CleanOptional(request.PortalImagePath);
                request.IsActive = NormalizeStatus(request.IsActive);

                if (await _groupRepository.GroupNameExistsAsync(request.PortalName))
                {
                    return Failure<GroupDto>("A group with this name already exists.");
                }

                GroupDto? group = await _groupRepository.CreateGroupAsync(request);
                return group == null
                    ? Failure<GroupDto>("Unable to create the group.")
                    : Success("Group created successfully.", group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create group {GroupName}.", request.PortalName);
                return Failure<GroupDto>("Unable to create the group.");
            }
        }

        public async Task<GroupManagementResponse<SubGroupDto>> CreateSubGroupAsync(CreateSubGroupRequest request)
        {
            try
            {
                request.UserTypeName = request.UserTypeName.Trim();
                request.UserTypeNameHn = CleanOptional(request.UserTypeNameHn);
                request.IsActive = NormalizeStatus(request.IsActive);

                if (!await _groupRepository.GroupExistsAsync(request.PortalId))
                {
                    return Failure<SubGroupDto>("The selected parent group no longer exists.");
                }

                if (await _groupRepository.SubGroupNameExistsAsync(request.PortalId, request.UserTypeName))
                {
                    return Failure<SubGroupDto>("This subgroup already exists for the selected group.");
                }

                SubGroupDto? subGroup = await _groupRepository.CreateSubGroupAsync(request);
                return subGroup == null
                    ? Failure<SubGroupDto>("Unable to create the subgroup.")
                    : Success("Subgroup created successfully.", subGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to create subgroup {SubGroupName} for portal {PortalId}.", request.UserTypeName, request.PortalId);
                return Failure<SubGroupDto>("Unable to create the subgroup.");
            }
        }

        private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string NormalizeStatus(string? value) => string.Equals(value, "N", StringComparison.OrdinalIgnoreCase) ? "N" : "Y";

        private static GroupManagementResponse<T> Success<T>(string message, T data) => new()
        {
            Status = true,
            Message = message,
            Data = data
        };

        private static GroupManagementResponse<T> Failure<T>(string message) => new()
        {
            Status = false,
            Message = message
        };
    }
}
