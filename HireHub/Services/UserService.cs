using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HireHub.API.DTOs;
using HireHub.API.Models;
using HireHub.API.Repositories.Interfaces;
using HireHub.API.Exceptions;
using HireHub.API.Utils;

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
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new NotFoundException($"User with id '{id}' not found.");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetByRoleAsync(string role)
        {
            var users = await _userRepository.GetByRoleAsync(role);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<UserDto>> SearchByNameAsync(string name)
        {
            var users = await _userRepository.SearchByNameAsync(name);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new DuplicateEmailException($"Email '{dto.Email}' is already registered.");

            var user = _mapper.Map<User>(dto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.UserId = Guid.NewGuid();
            user.IsActive = true;
            user.DeactivatedAt = null;

            var created = await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(created);
        }

        public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new NotFoundException($"User with id '{id}' not found.");
            _mapper.Map(dto, user);
            var updated = await _userRepository.UpdateAsync(user);
            return _mapper.Map<UserDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var deleted = await _userRepository.DeleteAsync(id);
            if (!deleted) throw new NotFoundException($"User with id '{id}' not found.");
            return true;
        }

        public async Task<bool> DeletePermanentlyAsync(Guid id)
        {
            var deleted = await _userRepository.DeletePermanentlyAsync(id);
            return deleted;
        }

        public async Task<string> ScheduleDeletionAsync(Guid userId, int days)
        {
            if (days <= 0) days = 30;
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new NotFoundException($"User with id '{userId}' not found.");
            var deletionAt = DateTime.UtcNow.AddDays(days);
            await _userRepository.ScheduleDeletionAsync(userId, deletionAt);
            return $"Account scheduled for deletion in {days} day(s) at {deletionAt:O}.";
        }

        public async Task<string> DeactivateAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new NotFoundException($"User with id '{userId}' not found.");
            await _userRepository.DeactivateAsync(userId);
            return "Account deactivated.";
        }

        public async Task<bool> DeactivateAccountAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || !user.IsActive) return false;
            user.IsActive = false;
            user.DeactivatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ReactivateAccountAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.IsActive) return false;
            user.IsActive = true;
            user.DeactivatedAt = null;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;
            var token = _tokenService.CreateToken(user.UserId, user.Role, user.Email);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
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
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return;
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
            var body = $"<p>Hi {user.FullName},</p><p>Reset your password by clicking this link:</p><p><a href=\"{link}\">{link}</a></p><p>If you didn't request this, you can ignore this email.</p>";
            await _emailService.SendAsync(user.Email, "Reset your password", body);
        }

        public async Task<bool> ResetPasswordWithTokenAsync(string rawToken, string newPassword)
        {
            var tokenHash = TokenUtils.Sha256Hex(rawToken);
            var reset = await _passwordResetRepository.GetByTokenHashAsync(tokenHash);
            if (reset == null || reset.ExpiresAt < DateTime.UtcNow) return false;
            var user = await _userRepository.GetByIdAsync(reset.UserId);
            if (user == null) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            await _passwordResetRepository.MarkUsedAsync(reset);
            return true;
        }
    }
}
