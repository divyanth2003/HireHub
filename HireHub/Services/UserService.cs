using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Exceptions; // NotFoundException, DuplicateEmailException

namespace HireHub.API.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ITokenService tokenService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        // ------------------- GET -------------------
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            _logger.LogInformation("Fetching all users");
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching user {UserId}", id);
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", id);
                throw new NotFoundException($"User with id '{id}' not found.");
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetByRoleAsync(string role)
        {
            _logger.LogInformation("Fetching users by role {Role}", role);
            var users = await _userRepository.GetByRoleAsync(role);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<UserDto>> SearchByNameAsync(string name)
        {
            _logger.LogInformation("Searching users with name containing {Name}", name);
            var users = await _userRepository.SearchByNameAsync(name);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        // ------------------- CREATE -------------------
        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            _logger.LogInformation("Creating user with email {Email}", dto.Email);

            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                _logger.LogWarning("Duplicate email registration attempted: {Email}", dto.Email);
                throw new DuplicateEmailException($"Email '{dto.Email}' is already registered.");
            }

            var user = _mapper.Map<User>(dto);

            // hash password + set system fields
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.UserId = Guid.NewGuid();

            try
            {
                var created = await _userRepository.AddAsync(user);
                _logger.LogInformation("User {UserId} created successfully", created.UserId);
                return _mapper.Map<UserDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email {Email}", dto.Email);
                throw;
            }
        }

        // ------------------- UPDATE -------------------
        public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            _logger.LogInformation("Updating user {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for update", id);
                throw new NotFoundException($"User with id '{id}' not found.");
            }

            _mapper.Map(dto, user);

            try
            {
                var updated = await _userRepository.UpdateAsync(user);
                _logger.LogInformation("User {UserId} updated successfully", id);
                return _mapper.Map<UserDto>(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                throw;
            }
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting user {UserId}", id);

            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("User {UserId} not found for deletion", id);
                throw new NotFoundException($"User with id '{id}' not found.");
            }

            _logger.LogInformation("User {UserId} deleted successfully", id);
            return true;
        }

        // ------------------- AUTH -------------------
        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Email}", dto.Email);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - invalid password for {Email}", dto.Email);
                return null;
            }

            var token = _tokenService.CreateToken(user.UserId, user.Role, user.Email);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            _logger.LogInformation("User {UserId} logged in successfully", user.UserId);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = jwt.ValidTo,
                Role = user.Role,
                UserId = user.UserId
            };
        }
    }
}
