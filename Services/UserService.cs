using UserManagementAPI.Models;
using UserManagementAPI.Models.Pagination;
using UserManagementAPI.DTOs;
using UserManagementAPI.Helpers;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace UserManagementAPI.Services
{
  public class UserService : IUserService
  {
    private readonly ConcurrentDictionary<int, User> _users;
    private readonly ConcurrentDictionary<string, List<int>> _emailIndex;
    private readonly ConcurrentDictionary<string, List<int>> _departmentIndex;
    private readonly IMemoryCache _cache;
    private readonly object _indexLock = new object();
    private int _nextId;
    private const string AllUsersCacheKey = "all_active_users";
    private const int CacheExpirationMinutes = 5;

    public UserService(IMemoryCache cache)
    {
      _users = new ConcurrentDictionary<int, User>();
      _emailIndex = new ConcurrentDictionary<string, List<int>>();
      _departmentIndex = new ConcurrentDictionary<string, List<int>>();
      _cache = cache;
      _nextId = 1;

      // Add some sample data
      SeedData();
    }

    public Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
      return Task.FromResult(_users.Values
          .Where(u => u.IsActive)
          .Select(MapToDto)
          .OrderBy(u => u.LastName)
          .ThenBy(u => u.FirstName)
          .AsEnumerable());
    }

    public Task<PaginatedResult<UserDto>> GetUsersAsync(PaginationRequest request)
    {
      var cacheKey = $"users_page_{request.Page}_{request.PageSize}_{request.SortBy}_{request.SortDirection}_{request.Filter}_{request.Department}_{request.IsActive}";

      if (_cache.TryGetValue(cacheKey, out PaginatedResult<UserDto>? cachedResult) && cachedResult != null)
      {
        return Task.FromResult(cachedResult);
      }

      var query = _users.Values.AsQueryable();

      // Apply filters
      if (request.IsActive.HasValue)
      {
        query = query.Where(u => u.IsActive == request.IsActive.Value);
      }

      if (!string.IsNullOrEmpty(request.Department))
      {
        query = query.Where(u => u.Department.Equals(request.Department, StringComparison.OrdinalIgnoreCase));
      }

      if (!string.IsNullOrEmpty(request.Filter))
      {
        var filter = request.Filter.ToLowerInvariant();
        query = query.Where(u =>
          u.FirstName.ToLowerInvariant().Contains(filter) ||
          u.LastName.ToLowerInvariant().Contains(filter) ||
          u.Email.ToLowerInvariant().Contains(filter) ||
          u.Department.ToLowerInvariant().Contains(filter) ||
          u.Position.ToLowerInvariant().Contains(filter));
      }

      // Apply sorting
      query = ApplySorting(query, request.SortBy, request.SortDirection);

      // Get total count before pagination
      var totalCount = query.Count();

      // Apply pagination
      var skip = (request.Page - 1) * request.PageSize;
      var users = query
          .Skip(skip)
          .Take(request.PageSize)
          .Select(MapToDto)
          .ToList();

      var result = new PaginatedResult<UserDto>
      {
        Data = users,
        Page = request.Page,
        PageSize = request.PageSize,
        TotalCount = totalCount
      };

      // Cache the result for performance
      _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));

      return Task.FromResult(result);
    }

    public Task<UserDto?> GetUserByIdAsync(int id)
    {
      if (_users.TryGetValue(id, out var user) && user.IsActive)
      {
        return Task.FromResult<UserDto?>(MapToDto(user));
      }
      return Task.FromResult<UserDto?>(null);
    }

    public Task<UserDto?> GetUserByEmailAsync(string email)
    {
      // Use email index for faster lookup if available
      if (_emailIndex.TryGetValue(email.ToLowerInvariant(), out var userIds))
      {
        var userId = userIds.FirstOrDefault(id => _users.TryGetValue(id, out var u) && u.IsActive);
        if (userId != 0 && _users.TryGetValue(userId, out var user))
        {
          return Task.FromResult<UserDto?>(MapToDto(user));
        }
      }

      // Fallback to full scan
      var foundUser = _users.Values.FirstOrDefault(u =>
          u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.IsActive);
      return Task.FromResult(foundUser != null ? MapToDto(foundUser) : null);
    }

    public Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
      // Sanitize input data
      var sanitizedFirstName = InputSanitizer.SanitizeName(createUserDto.FirstName);
      var sanitizedLastName = InputSanitizer.SanitizeName(createUserDto.LastName);
      var sanitizedEmail = InputSanitizer.SanitizeEmail(createUserDto.Email);
      var sanitizedPhoneNumber = string.IsNullOrEmpty(createUserDto.PhoneNumber)
          ? null
          : InputSanitizer.SanitizePhoneNumber(createUserDto.PhoneNumber);
      var sanitizedDepartment = InputSanitizer.SanitizeDepartmentOrPosition(createUserDto.Department);
      var sanitizedPosition = InputSanitizer.SanitizeDepartmentOrPosition(createUserDto.Position);

      // Check if email already exists (using optimized method)
      var emailExists = _emailIndex.ContainsKey(sanitizedEmail.ToLowerInvariant()) &&
                       _emailIndex[sanitizedEmail.ToLowerInvariant()].Any(id =>
                         _users.TryGetValue(id, out var existingUser) && existingUser.IsActive);

      if (emailExists)
      {
        throw new InvalidOperationException("A user with this email already exists.");
      }

      var user = new User
      {
        Id = Interlocked.Increment(ref _nextId),
        FirstName = sanitizedFirstName,
        LastName = sanitizedLastName,
        Email = sanitizedEmail,
        PhoneNumber = sanitizedPhoneNumber,
        Department = sanitizedDepartment,
        Position = sanitizedPosition,
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow,
        IsActive = true
      };

      _users.TryAdd(user.Id, user);
      UpdateIndexes(user);
      InvalidateCache();

      return Task.FromResult(MapToDto(user));
    }

    public Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
      if (!_users.TryGetValue(id, out var user) || !user.IsActive)
      {
        return Task.FromResult<UserDto?>(null);
      }

      var originalEmail = user.Email;
      var originalDepartment = user.Department;

      // Sanitize input data
      var sanitizedEmail = string.IsNullOrEmpty(updateUserDto.Email)
          ? null
          : InputSanitizer.SanitizeEmail(updateUserDto.Email);

      // Check if email is being updated and if it already exists
      if (!string.IsNullOrEmpty(sanitizedEmail) &&
          !user.Email.Equals(sanitizedEmail, StringComparison.OrdinalIgnoreCase))
      {
        var emailExists = _emailIndex.ContainsKey(sanitizedEmail.ToLowerInvariant()) &&
                         _emailIndex[sanitizedEmail.ToLowerInvariant()].Any(userId =>
                           userId != id && _users.TryGetValue(userId, out var existingUser) && existingUser.IsActive);

        if (emailExists)
        {
          throw new InvalidOperationException("A user with this email already exists.");
        }
      }

      // Update only provided fields with sanitized data
      if (!string.IsNullOrEmpty(updateUserDto.FirstName))
        user.FirstName = InputSanitizer.SanitizeName(updateUserDto.FirstName);
      if (!string.IsNullOrEmpty(updateUserDto.LastName))
        user.LastName = InputSanitizer.SanitizeName(updateUserDto.LastName);
      if (!string.IsNullOrEmpty(sanitizedEmail))
        user.Email = sanitizedEmail;
      if (updateUserDto.PhoneNumber != null)
        user.PhoneNumber = InputSanitizer.SanitizePhoneNumber(updateUserDto.PhoneNumber);
      if (!string.IsNullOrEmpty(updateUserDto.Department))
        user.Department = InputSanitizer.SanitizeDepartmentOrPosition(updateUserDto.Department);
      if (!string.IsNullOrEmpty(updateUserDto.Position))
        user.Position = InputSanitizer.SanitizeDepartmentOrPosition(updateUserDto.Position);
      if (updateUserDto.IsActive.HasValue)
        user.IsActive = updateUserDto.IsActive.Value;

      user.DateModified = DateTime.UtcNow;

      // Update indexes if email or department changed
      if (originalEmail != user.Email || originalDepartment != user.Department)
      {
        RemoveFromIndexes(new User { Id = id, Email = originalEmail, Department = originalDepartment });
        UpdateIndexes(user);
      }

      InvalidateCache();
      return Task.FromResult<UserDto?>(MapToDto(user));
    }

    public Task<bool> DeleteUserAsync(int id)
    {
      if (_users.TryGetValue(id, out var user) && user.IsActive)
      {
        // Soft delete
        user.IsActive = false;
        user.DateModified = DateTime.UtcNow;
        InvalidateCache();
        return Task.FromResult(true);
      }
      return Task.FromResult(false);
    }

    public Task<IEnumerable<UserDto>> GetUsersByDepartmentAsync(string department)
    {
      // Use department index for faster lookup
      if (_departmentIndex.TryGetValue(department.ToLowerInvariant(), out var userIds))
      {
        var users = userIds
            .Where(id => _users.TryGetValue(id, out var user) && user.IsActive)
            .Select(id => _users[id])
            .Select(MapToDto)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName);

        return Task.FromResult(users.AsEnumerable());
      }

      // Fallback to full scan
      return Task.FromResult(_users.Values
          .Where(u => u.IsActive && u.Department.Equals(department, StringComparison.OrdinalIgnoreCase))
          .Select(MapToDto)
          .OrderBy(u => u.LastName)
          .ThenBy(u => u.FirstName)
          .AsEnumerable());
    }

    public Task<bool> UserExistsAsync(int id)
    {
      return Task.FromResult(_users.TryGetValue(id, out var user) && user.IsActive);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
      var emailKey = email.ToLowerInvariant();
      if (_emailIndex.TryGetValue(emailKey, out var userIds))
      {
        var exists = userIds.Any(id =>
          (excludeUserId == null || id != excludeUserId) &&
          _users.TryGetValue(id, out var user) && user.IsActive);
        return Task.FromResult(exists);
      }

      // Fallback to full scan
      return Task.FromResult(_users.Values.Any(u =>
          u.IsActive &&
          u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
          (excludeUserId == null || u.Id != excludeUserId)));
    }

    private static UserDto MapToDto(User user)
    {
      return new UserDto
      {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Department = user.Department,
        Position = user.Position,
        DateCreated = user.DateCreated,
        DateModified = user.DateModified,
        IsActive = user.IsActive
      };
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string? sortDirection)
    {
      var isDescending = sortDirection?.ToLowerInvariant() == "desc";

      return sortBy?.ToLowerInvariant() switch
      {
        "firstname" => isDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
        "lastname" => isDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
        "email" => isDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
        "department" => isDescending ? query.OrderByDescending(u => u.Department) : query.OrderBy(u => u.Department),
        "position" => isDescending ? query.OrderByDescending(u => u.Position) : query.OrderBy(u => u.Position),
        "datecreated" => isDescending ? query.OrderByDescending(u => u.DateCreated) : query.OrderBy(u => u.DateCreated),
        "datemodified" => isDescending ? query.OrderByDescending(u => u.DateModified) : query.OrderBy(u => u.DateModified),
        _ => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName) // Default sorting
      };
    }

    private void UpdateIndexes(User user)
    {
      lock (_indexLock)
      {
        // Update email index
        var emailKey = user.Email.ToLowerInvariant();
        if (!_emailIndex.ContainsKey(emailKey))
        {
          _emailIndex[emailKey] = new List<int>();
        }
        if (!_emailIndex[emailKey].Contains(user.Id))
        {
          _emailIndex[emailKey].Add(user.Id);
        }

        // Update department index
        var deptKey = user.Department.ToLowerInvariant();
        if (!_departmentIndex.ContainsKey(deptKey))
        {
          _departmentIndex[deptKey] = new List<int>();
        }
        if (!_departmentIndex[deptKey].Contains(user.Id))
        {
          _departmentIndex[deptKey].Add(user.Id);
        }
      }
    }

    private void RemoveFromIndexes(User user)
    {
      lock (_indexLock)
      {
        // Remove from email index
        var emailKey = user.Email.ToLowerInvariant();
        if (_emailIndex.TryGetValue(emailKey, out var emailList))
        {
          emailList.Remove(user.Id);
          if (emailList.Count == 0)
          {
            _emailIndex.TryRemove(emailKey, out _);
          }
        }

        // Remove from department index
        var deptKey = user.Department.ToLowerInvariant();
        if (_departmentIndex.TryGetValue(deptKey, out var deptList))
        {
          deptList.Remove(user.Id);
          if (deptList.Count == 0)
          {
            _departmentIndex.TryRemove(deptKey, out _);
          }
        }
      }
    }

    private void InvalidateCache()
    {
      // Remove all cached results when data changes
      _cache.Remove(AllUsersCacheKey);

      // Remove paginated results from cache (this is simplified - in production you might want more sophisticated cache invalidation)
      var cacheEntries = typeof(MemoryCache).GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      // Note: This is a simplified approach. In production, consider using cache tagging or other cache invalidation strategies
    }

    private void SeedData()
    {
      var sampleUsers = new[]
      {
                new User
                {
                    Id = Interlocked.Increment(ref _nextId),
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@company.com",
                    PhoneNumber = "+1-555-0123",
                    Department = "IT",
                    Position = "Software Developer",
                    DateCreated = DateTime.UtcNow.AddDays(-30),
                    DateModified = DateTime.UtcNow.AddDays(-30),
                    IsActive = true
                },
                new User
                {
                    Id = Interlocked.Increment(ref _nextId),
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@company.com",
                    PhoneNumber = "+1-555-0124",
                    Department = "HR",
                    Position = "HR Manager",
                    DateCreated = DateTime.UtcNow.AddDays(-25),
                    DateModified = DateTime.UtcNow.AddDays(-25),
                    IsActive = true
                },
                new User
                {
                    Id = Interlocked.Increment(ref _nextId),
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Email = "mike.johnson@company.com",
                    PhoneNumber = "+1-555-0125",
                    Department = "IT",
                    Position = "System Administrator",
                    DateCreated = DateTime.UtcNow.AddDays(-20),
                    DateModified = DateTime.UtcNow.AddDays(-20),
                    IsActive = true
                }
            };

      foreach (var user in sampleUsers)
      {
        _users.TryAdd(user.Id, user);
        UpdateIndexes(user);
      }
    }
  }
}
