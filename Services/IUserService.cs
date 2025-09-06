using UserManagementAPI.Models;
using UserManagementAPI.Models.Pagination;
using UserManagementAPI.DTOs;

namespace UserManagementAPI.Services
{
  public interface IUserService
  {
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<PaginatedResult<UserDto>> GetUsersAsync(PaginationRequest request);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<UserDto>> GetUsersByDepartmentAsync(string department);
    Task<bool> UserExistsAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
  }
}
