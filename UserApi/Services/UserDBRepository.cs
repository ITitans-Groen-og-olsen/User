using MongoDB.Driver;
using MongoDB.Bson.Serialization;
namespace UserApi.Services;
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
        var connectionString = configuration["MongoConnectionString"] ?? "<blank>";
        var databaseName = configuration["DatabaseName"] ?? "<blank>";
        var collectionName = configuration["CollectionName"] ?? "<blank>";
        var collectionNameLogin = configuration["CollectionNameLogin"] ?? "<blank>";
        _logger.LogInformation($"Connected to MongoDB using: {connectionString}");
        _logger.LogInformation($" Using database: {databaseName}");
        _logger.LogInformation($" Using Collection: {collectionName}");
        _logger.LogInformation($" Using Collection: {collectionNameLogin}");
        try
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _userCollection = database.GetCollection<User>(collectionName);
            _loginCollection = database.GetCollection<Login>(collectionNameLogin);
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

            Login login = new Login
            {
                Emailaddress = user.EmailAddress,
                Password = user.Password
            };
            
            await _userCollection.InsertOneAsync(user);
            await _loginCollection.InsertOneAsync(login);
            _logger.LogInformation($"User with ID {user.Id} created successfully.");
            _logger.LogInformation($"Login {login.Emailaddress} created successfully.");
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

            var result = await _userCollection.ReplaceOneAsync(u => u.Id.ToString() == id, updatedUser);
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
}