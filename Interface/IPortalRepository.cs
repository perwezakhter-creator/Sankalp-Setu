using System.Collections.Generic;
using System.Threading.Tasks;
using SankalpAPI.Model;

namespace SankalpAPI.Interface
{
    public interface IPortalRepository
    {
        Task<List<PortalDto>> GetActivePortalsAsync();
      
        Task<PortalDto?> GetPortalByIdAsync(long portalId);
        Task<List<UserTypeDto>> GetUserTypeByIdAsync(long portalId);
        Task<PortalUserMappingDto?> GetPortalAndUserMappingAsync(long portalId, string? mobile);
        Task<List<MapUserPortalDto>> GetMapUserPortalAsync(long? portalId, long? userTypeId);

    }
}
