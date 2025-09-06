using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models.Pagination;
using UserManagementAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Produces("application/json")]
  public class UsersController : ControllerBase
  {
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
      _userService = userService;
      _logger = logger;
    }

    /// <summary>
    /// Gets all active users (legacy endpoint - use GetUsers for better performance)
    /// </summary>
    /// <returns>List of all active users</returns>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
      try
      {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while retrieving all users");
        return StatusCode(500, "An error occurred while retrieving users");
      }
    }

    /// <summary>
    /// Gets users with pagination, filtering, and sorting support
    /// </summary>
    /// <param name="request">Pagination and filtering parameters</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<UserDto>>> GetUsers([FromQuery] PaginationRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return BadRequest(ModelState);
        }

        var result = await _userService.GetUsersAsync(request);
        return Ok(result);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while retrieving users with pagination");
        return StatusCode(500, "An error occurred while retrieving users");
      }
    }

    /// <summary>
    /// Gets a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
      try
      {
        // Validate ID parameter
        if (id <= 0)
        {
          return BadRequest("User ID must be a positive integer");
        }

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
          return NotFound($"User with ID {id} not found");
        }
        return Ok(user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while retrieving user {UserId}", id);
        return StatusCode(500, "An error occurred while retrieving the user");
      }
    }

    /// <summary>
    /// Gets a user by email address
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>User details</returns>
    [HttpGet("by-email")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> GetUserByEmail([FromQuery, Required, EmailAddress] string email)
    {
      try
      {
        if (string.IsNullOrEmpty(email))
        {
          return BadRequest("Email parameter is required");
        }

        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
          return NotFound($"User with email {email} not found");
        }
        return Ok(user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while retrieving user by email {Email}", email);
        return StatusCode(500, "An error occurred while retrieving the user");
      }
    }

    /// <summary>
    /// Gets all users in a specific department
    /// </summary>
    /// <param name="department">Department name</param>
    /// <returns>List of users in the department</returns>
    [HttpGet("by-department/{department}")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByDepartment(string department)
    {
      try
      {
        // Validate department parameter
        if (string.IsNullOrWhiteSpace(department))
        {
          return BadRequest("Department parameter cannot be null or empty");
        }

        if (department.Length > 100)
        {
          return BadRequest("Department parameter cannot exceed 100 characters");
        }

        var users = await _userService.GetUsersByDepartmentAsync(department);
        return Ok(users);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while retrieving users by department {Department}", department);
        return StatusCode(500, "An error occurred while retrieving users");
      }
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          return BadRequest(ModelState);
        }

        var user = await _userService.CreateUserAsync(createUserDto);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Conflict occurred while creating user with email {Email}", createUserDto.Email);
        return Conflict(ex.Message);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while creating user with email {Email}", createUserDto.Email);
        return StatusCode(500, "An error occurred while creating the user");
      }
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">User update data</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
      try
      {
        // Validate ID parameter
        if (id <= 0)
        {
          return BadRequest("User ID must be a positive integer");
        }

        if (!ModelState.IsValid)
        {
          return BadRequest(ModelState);
        }

        // Check if at least one field is provided for update
        if (IsUpdateDtoEmpty(updateUserDto))
        {
          return BadRequest("At least one field must be provided for update");
        }

        var user = await _userService.UpdateUserAsync(id, updateUserDto);
        if (user == null)
        {
          return NotFound($"User with ID {id} not found");
        }

        return Ok(user);
      }
      catch (InvalidOperationException ex)
      {
        _logger.LogWarning(ex, "Conflict occurred while updating user {UserId}", id);
        return Conflict(ex.Message);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while updating user {UserId}", id);
        return StatusCode(500, "An error occurred while updating the user");
      }
    }

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(int id)
    {
      try
      {
        // Validate ID parameter
        if (id <= 0)
        {
          return BadRequest("User ID must be a positive integer");
        }

        var result = await _userService.DeleteUserAsync(id);
        if (!result)
        {
          return NotFound($"User with ID {id} not found");
        }

        return NoContent();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while deleting user {UserId}", id);
        return StatusCode(500, "An error occurred while deleting the user");
      }
    }

    /// <summary>
    /// Checks if a user exists
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Boolean indicating if user exists</returns>
    [HttpHead("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UserExists(int id)
    {
      try
      {
        // Validate ID parameter
        if (id <= 0)
        {
          return BadRequest("User ID must be a positive integer");
        }

        var exists = await _userService.UserExistsAsync(id);
        return exists ? Ok() : NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while checking if user {UserId} exists", id);
        return StatusCode(500);
      }
    }

    /// <summary>
    /// Helper method to check if UpdateUserDto is empty
    /// </summary>
    /// <param name="dto">The UpdateUserDto to check</param>
    /// <returns>True if all fields are null or empty</returns>
    private static bool IsUpdateDtoEmpty(UpdateUserDto dto)
    {
      return string.IsNullOrWhiteSpace(dto.FirstName) &&
             string.IsNullOrWhiteSpace(dto.LastName) &&
             string.IsNullOrWhiteSpace(dto.Email) &&
             string.IsNullOrWhiteSpace(dto.PhoneNumber) &&
             string.IsNullOrWhiteSpace(dto.Department) &&
             string.IsNullOrWhiteSpace(dto.Position) &&
             !dto.IsActive.HasValue;
    }
  }
}
