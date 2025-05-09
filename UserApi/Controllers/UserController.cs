using Microsoft.AspNetCore.Mvc;
using UserApi.Models;
using System.Linq;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using UserApi.Services;
using System.ComponentModel;

namespace UserApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    /*
    private static List<User> _users = new List<User>() {
        new () {
            Id = new Guid("c9fcbc4b-d2d1-4664-9079-dae78a1de446"),
            Name = "Henrik Fisker",
            Address1 = "Søndergade 3",
            City = "Harboøre",
            PostalCode = 7673,
            EmailAddress = "hnrk@afiskbutik.dk",
            PhoneNumber = "133466789"
        }
    };
    */

    private readonly ILogger<UserController> _logger;
    private readonly IUserDBRepository _userMongoDBRepository;

    public UserController(ILogger<UserController> logger, IUserDBRepository userDBRepository)
    {
        _logger = logger;
        _userMongoDBRepository = userDBRepository;

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"XYZ Service responding from {_ipaddr}");
    }

    [HttpPost(Name = "posttest")]
    public Task<User> test([FromBody] User user)
    {
        try
        {
            return _userMongoDBRepository.GetUserByIdAsync(user.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

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

    [HttpPost("AddUser")]
    public Task<User> AddUser([FromBody] User user)
    {
        try
        {
            return _userMongoDBRepository.CreateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
    
    [HttpPost("Login")]
    public Task<User> Login([FromBody] User user)
    {
        try
        {
            return _userMongoDBRepository.CreateUserAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

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

    [HttpGet("version")]
    public async Task<Dictionary<string, string>> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;
        properties.Add("service", "HaaV User Service");
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program)
        .Assembly.Location).ProductVersion;
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