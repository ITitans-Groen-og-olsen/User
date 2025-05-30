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
    private readonly IMongoCollection<Login>? _loginCollection;
    private readonly IMongoCollection<User>? _userCollection;
    private readonly ILogger<UserDBRepository> _logger;

    // Repository constructer
    public UserDBRepository(ILogger<UserDBRepository> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Fetch database information from environment
        // Falls back to using config if envirinment doesnt contain the right variable
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

        // Found database information is used to setup the mongodb client
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

    // Adds given user to database
    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            // Checks if user is missing
            if (user == null)
            {
                _logger.LogWarning("Attempted to create a null user.");
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            // Finds the user with the highest customer number
            var filter = Builders<User>.Filter.Empty;
            var foundUser = await _userCollection
                .Find(filter)
                .SortByDescending(u => u.CustomerNumber)
                .FirstOrDefaultAsync();

            // Customer number for the new user is set to 1 higher than the highest customer number
            if (foundUser != null)
            {
                user.CustomerNumber = foundUser.CustomerNumber + 1;
                Console.WriteLine($"New customer number set to {user.CustomerNumber}");
                _logger.LogInformation($"User with ID {user.Id} created successfully.");
            }
            // If no user is found the customer is given customer number 1
            else
            {
                user.CustomerNumber = 1;
            }

            // User is inserted into the database
            await _userCollection.InsertOneAsync(user);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }

    // Fetches user with specific id from the database
    public async Task<User> GetUserByIdAsync(string id)
    {
        try
        {
            // Check if the id is valid
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for GetUserByIdAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }
            // Create filter and find user matching the id
            var filter = Builders<User>.Filter.Eq("_id", id);
            var user = await _userCollection.Find(filter).FirstOrDefaultAsync();
            // Make sure user is not null
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

    // Fetch all users in the database
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        // Use empty filter to fetch every user
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

    // Takes new user and updates existing users fields
    public async Task<User> UpdateUserAsync(string id, User updatedUser)
    {
        try
        {
            // Make sure id is not null
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for UpdateUserAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }
            // Make sure user is not null
            if (updatedUser == null)
            {
                _logger.LogWarning("Attempted to update with a null user.");
                throw new ArgumentNullException(nameof(updatedUser), "Updated user cannot be null");
            }

            // Replace the user matching id with the given user
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

    // Remove user from database
    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            // Make sure the id is not null
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid ID provided for DeleteUserAsync.");
                throw new ArgumentException("Invalid ID", nameof(id));
            }

            // Delete the user matching the given id from database
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

    // Validates login credentials
    public async Task<IActionResult> Login(Login login)
    {
        try
        {
            // Make sure both the email and password arent null
            if (login.Password == null || login.EmailAddress == null)
            {
                _logger.LogWarning("Login null or not found");
                throw new ArgumentNullException(nameof(login), "Login is null or was not found");
            }
            // Initialize new object for sending response back to the controller
            Response response = new();

            // Hash password before its saved to the database
            string hashed = HashPassword(login);

            // Find user matching the given login
            var filter = Builders<User>.Filter.Eq("EmailAddress", login.EmailAddress);
            var foundUser = await _userCollection.Find(filter).FirstOrDefaultAsync();
            Console.WriteLine(
                $"Incoming password hashed to {hashed}. Found password is {foundUser.Password}"
            );

            // Check if the hashed password of the found user matches the hashed password of the give user
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

    // Hashes passwords using static salt
    public string HashPassword(Login login)
    {
        // Static salt, would be more secure to generate for each password seperately
        // For further development, this would be saved inside the user obejct and be uniqe for every user
        string salt = "3/0D9TaEelBiIHxKfuX3ng==";

        // Hashing algorithm for scrambles password
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

// custom object for returning login status and user id
public class Response
{
    public string id { get; set; }
    public string loginResult { get; set; }
}
