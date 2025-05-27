using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace UserApi.Services;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using UserApi.Models;

public class UserDBRepository : IUserDBRepository
{
    // mongodb://admin:1234@localhost:27018/UserDB?authSource=admin
    private readonly IMongoCollection<Login>? _loginCollection;
    private readonly IMongoCollection<User>? _userCollection;
    private readonly ILogger<UserDBRepository> _logger;

    public UserDBRepository(ILogger<UserDBRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        var connectionString =
            Environment.GetEnvironmentVariable("MongoConnectionString")
            ?? configuration.GetValue<string>("MongoConnectionString");

        var databaseName =
            Environment.GetEnvironmentVariable("DatabaseName")
            ?? configuration.GetValue<string>("DatabaseName", "UserDBEksamen");

        var collectionName =
            Environment.GetEnvironmentVariable("CollectionName")
            ?? configuration.GetValue<string>("CollectionName", "Users");
        _logger.LogInformation($"Connected to MongoDB using: {connectionString}");
        _logger.LogInformation($" Using database: {databaseName}");
        _logger.LogInformation($" Using Collection: {collectionName}");
        try
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _userCollection = database.GetCollection<User>(collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to connect to MongoDB: {0}", ex.Message);
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            if (user == null)
            {
                _logger.LogWarning("Attempted to create a null user.");
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }
            var filter = Builders<User>.Filter.Empty;
            var foundUser = await _userCollection
                .Find(filter)
                .SortByDescending(u => u.CustomerNumber)
                .FirstOrDefaultAsync();
            Console.WriteLine(
                $"Customernumber: {user.CustomerNumber} replaced with Customernumver{foundUser.CustomerNumber}"
            );
            user.CustomerNumber = foundUser.CustomerNumber + 1;
            Console.WriteLine($"New customer number set to {user.CustomerNumber}");
            await _userCollection.InsertOneAsync(user);
            _logger.LogInformation($"User with ID {user.Id} created successfully.");
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for GetUserByIdAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }
            var filter = Builders<User>.Filter.Eq("_id", id);
            var user = await _userCollection.Find(filter).FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.LogWarning($"User with ID {id} not found.");
            }
            else
            {
                _logger.LogInformation($"User with ID {id} retrieved successfully.");
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        try
        {
            var users = await _userCollection.Find(_ => true).ToListAsync();
            _logger.LogInformation($"Retrieved {users.Count} users.");
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(string id, User updatedUser)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for UpdateUserAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }

            if (updatedUser == null)
            {
                _logger.LogWarning("Attempted to update with a null user.");
                throw new ArgumentNullException(nameof(updatedUser), "Updated user cannot be null");
            }

            var result = await _userCollection.ReplaceOneAsync(
                u => u.Id.ToString() == id,
                updatedUser
            );
            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation($"User with ID {id} updated successfully.");
                return updatedUser;
            }
            else
            {
                _logger.LogWarning($"User with ID {id} not found or no changes made.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for DeleteUserAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }

            var result = await _userCollection.DeleteOneAsync(u => u.Id.ToString() == id);
            if (result.DeletedCount > 0)
            {
                _logger.LogInformation($"User with ID {id} deleted successfully.");
                return true;
            }
            else
            {
                _logger.LogWarning($"User with ID {id} not found.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public async Task<IActionResult> Login(Login login)
    {
        try
        {
            if (login.Password == null || login.EmailAddress == null)
            {
                _logger.LogWarning("Login null or not found");
                throw new ArgumentNullException(nameof(login), "Login is null or was not found");
            }
            Response response = new();
            string hashed = HashPassword(login);
            var filter = Builders<User>.Filter.Eq("EmailAddress", login.EmailAddress);
            var foundUser = await _userCollection.Find(filter).FirstOrDefaultAsync();
            Console.WriteLine(
                $"Incoming password hashed to {hashed}. Found password is {foundUser.Password}"
            );
            if (foundUser != null && foundUser.Password == hashed)
            {
                response.id = foundUser.Id.ToString();
                response.loginResult = "true";

                return new OkObjectResult(response);
            }
            else
            {
                response.id = "";
                response.loginResult = "false";
                return new OkObjectResult(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    public string HashPassword(Login login)
    {
        string salt = "3/0D9TaEelBiIHxKfuX3ng==";
        string hashed = Convert.ToBase64String(
            KeyDerivation.Pbkdf2(
                password: login.Password,
                salt: Convert.FromBase64String(salt),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            )
        );
        Console.WriteLine(
            $"Hashing was called and produced: {hashed} using password {login.Password}"
        );
        return hashed;
    }
}

public class Response
{
    public string id { get; set; }
    public string loginResult { get; set; }
}
