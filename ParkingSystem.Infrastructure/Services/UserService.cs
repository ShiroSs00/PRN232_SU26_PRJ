using MongoDB.Bson;
using MongoDB.Driver;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly MongoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public UserService(
        MongoDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .Find(u => u.Email == loginDto.Email)
            .FirstOrDefaultAsync();

        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _tokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            AccessToken = token,
            User = MapToDto(user)
        };
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await _context.Users
            .Find(u => u.Id == id)
            .FirstOrDefaultAsync();

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync();

        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _context.Users
            .Find(Builders<User>.Filter.Empty)
            .ToListAsync();

        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
    {
        var emailExists = await _context.Users
            .Find(u => u.Email == createUserDto.Email)
            .AnyAsync();

        if (emailExists)
        {
            throw new Exception($"Email '{createUserDto.Email}' is already registered.");
        }

        // Validate roles
        var allowedRoles = new[] { UserRoles.Admin, UserRoles.FacilityManager, UserRoles.ParkingStaff, UserRoles.Driver };
        if (createUserDto.Roles == null || createUserDto.Roles.Count == 0)
        {
            throw new Exception("At least one role is required.");
        }
        foreach (var r in createUserDto.Roles)
        {
            if (!allowedRoles.Contains(r))
            {
                throw new Exception($"Role '{r}' is invalid.");
            }
        }

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = createUserDto.FullName,
            Email = createUserDto.Email,
            PasswordHash = _passwordHasher.HashPassword(createUserDto.Password),
            PhoneNumber = createUserDto.PhoneNumber,
            Roles = createUserDto.Roles,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);

        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(string id, UpdateUserDto updateUserDto)
    {
        var user = await _context.Users
            .Find(u => u.Id == id)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
        {
            var emailExists = await _context.Users
                .Find(u => u.Email == updateUserDto.Email)
                .AnyAsync();

            if (emailExists)
            {
                throw new Exception($"Email '{updateUserDto.Email}' is already in use by another account.");
            }

            user.Email = updateUserDto.Email;
        }

        user.FullName = updateUserDto.FullName;
        user.PhoneNumber = updateUserDto.PhoneNumber;

        if (updateUserDto.Roles != null && updateUserDto.Roles.Count > 0)
        {
            var allowedRoles = new[] { UserRoles.Admin, UserRoles.FacilityManager, UserRoles.ParkingStaff, UserRoles.Driver };
            foreach (var r in updateUserDto.Roles)
            {
                if (!allowedRoles.Contains(r))
                {
                    throw new Exception($"Role '{r}' is invalid.");
                }
            }
            user.Roles = updateUserDto.Roles;
        }

        if (updateUserDto.IsActive.HasValue)
        {
            user.IsActive = updateUserDto.IsActive.Value;
        }

        if (!string.IsNullOrEmpty(updateUserDto.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(updateUserDto.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.Users.ReplaceOneAsync(u => u.Id == id, user);

        return MapToDto(user);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<User>.Update
            .Set(u => u.IsActive, false)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _context.Users.UpdateOneAsync(u => u.Id == id, update);

        return result.ModifiedCount > 0;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = user.Roles,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
