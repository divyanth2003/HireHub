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
using HireHub.API.Exceptions; 
using HireHub.API.Utils; 
using HireHub.API.Services; 

namespace HireHub.API.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly IEmailService _emailService;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            ITokenService tokenService,
            ILogger<UserService> logger,
            IPasswordResetRepository passwordResetRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
            _passwordResetRepository = passwordResetRepository;
            _emailService = emailService;
        }

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

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            _logger.LogInformation("Creating user with email {Email}", dto.Email);

            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                _logger.LogWarning("Duplicate email registration attempted: {Email}", dto.Email);
                throw new DuplicateEmailException($"Email '{dto.Email}' is already registered.");
            }

            var user = _mapper.Map<User>(dto);

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

    
        public async Task RequestPasswordResetAsync(string email, string originBaseUrl)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    
                    _logger.LogInformation("Password reset requested for unknown email {Email}", email);
                    return;
                }

                var rawToken = TokenUtils.CreateRawTokenBase64Url(32);
                var tokenHash = TokenUtils.Sha256Hex(rawToken);

                var reset = new PasswordReset
                {
                    UserId = user.UserId,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddHours(2),
                    CreatedAt = DateTime.UtcNow,
                    Used = false
                };

                await _passwordResetRepository.AddAsync(reset);

                var link = $"{originBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
                var body = $"<p>Hi {user.FullName},</p><p>Reset your password by clicking this link:</p><p><a href=\"{link}\">{link}</a></p>"
                         + "<p>If you didn't request this, you can ignore this email.</p>";

                try
                {
                    await _emailService.SendAsync(user.Email, "Reset your password", body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestPasswordResetAsync failed for {Email}", email);
              
            }
        }

        
        public async Task<bool> ResetPasswordWithTokenAsync(string rawToken, string newPassword)
        {
            try
            {
                var tokenHash = TokenUtils.Sha256Hex(rawToken);
                var reset = await _passwordResetRepository.GetByTokenHashAsync(tokenHash);
                if (reset == null)
                {
                    _logger.LogInformation("Password reset attempted with invalid token hash");
                    return false;
                }

                if (reset.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogInformation("Password reset attempted with expired token for UserId {UserId}", reset.UserId);
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(reset.UserId);
                if (user == null)
                {
                    _logger.LogWarning("Password reset: user not found for token (UserId {UserId})", reset.UserId);
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _userRepository.UpdateAsync(user);
                await _passwordResetRepository.MarkUsedAsync(reset);

                _logger.LogInformation("Password reset successful for UserId {UserId}", user.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPasswordWithTokenAsync failed");
                return false;
            }
        }
    }
}
