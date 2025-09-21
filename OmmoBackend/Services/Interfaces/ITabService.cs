using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface ITabService
    {
        Task<ServiceResponse<TabsResponseDto>> GetTabsAsync(int roleId);
    }
}