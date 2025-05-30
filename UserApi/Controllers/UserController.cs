using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserApi.Models;
using UserApi.Services;

namespace UserApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserDBRepository _userMongoDBRepository;

    // Constructer for user controller
    public UserController(ILogger<UserController> logger, IUserDBRepository userDBRepository)
    {
        _logger = logger;
        _userMongoDBRepository = userDBRepository;

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"XYZ Service responding from {_ipaddr}");
    }

    //Fetches specific user based on id
    [HttpGet("GetUserById/{userId}")]
    public Task<User> Get(string userId)
    {
        try
        {
            return _userMongoDBRepository.GetUserByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Fetches all users in database
    [HttpGet("GetAllUsers")]
    public Task<IEnumerable<User>> GetAllUsers()
    {
        try
        {
            return _userMongoDBRepository.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Adds a user to the database
    [HttpPost("AddUser")]
    public Task<User> AddUser([FromBody] User user)
    {
        try
        {
            Login login = new();
            login.EmailAddress = user.EmailAddress;
            login.Password = user.Password;

            user.Password = _userMongoDBRepository.HashPassword(login).ToString();
            Console.WriteLine($"Password set to: {user.Password}");
            return _userMongoDBRepository.CreateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Updates existing user with data from incoming user
    [HttpPut("UpdateUser/{userId}")]
    public Task<User> UpdateUser(string userId, User user)
    {
        try
        {
            return _userMongoDBRepository.UpdateUserAsync(userId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Deletes user based on given id
    [HttpDelete("DeleteUser/{userId}")]
    public Task<bool> DeleteUser(string userId)
    {
        try
        {
            return _userMongoDBRepository.DeleteUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Accepts login info and returns authorized/unauthorized based on validity of credentials
    [HttpPost("login")]
    public Task<IActionResult> Login(Login login)
    {
        try
        {
            return _userMongoDBRepository.Login(login);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;
        properties.Add("service", "User Service");
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
        properties.Add("version", ver!);
        try
        {
            var hostName = System.Net.Dns.GetHostName();
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipa = ips.First().MapToIPv4().ToString();
            properties.Add("hosted-at-address", ipa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            properties.Add("hosted-at-address", "Could not resolve IP-address");
        }
        return properties;
    }
}
