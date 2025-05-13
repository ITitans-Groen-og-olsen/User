using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using UserApi.Controllers;
using UserApi.Models;
using UserApi.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserApi.Test
{
    [TestClass]
    public class UserControllerTests
    {
        private Mock<ILogger<UserController>> _mockLogger = null!;
        private Mock<IUserDBRepository> _mockUserRepository = null!;
        private UserController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<UserController>>();
            _mockUserRepository = new Mock<IUserDBRepository>();
            _controller = new UserController(_mockLogger.Object, _mockUserRepository.Object);
        }

        [TestMethod]
        public async Task GetUserById_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var user = new User { Id = Guid.Parse(userId), FirstName = "Test User", EmailAddress = "test@example.com" };

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Get(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.Id.ToString());
            Assert.AreEqual("Test User", result.FirstName);
        }

        [TestMethod]
        public async Task AddUser_ReturnsCreatedUser()
        {
            // Arrange
            var newUser = new User { Id = Guid.NewGuid(), FirstName = "New User", EmailAddress = "new@example.com" };

            _mockUserRepository
                .Setup(repo => repo.CreateUserAsync(newUser))
                .ReturnsAsync(newUser);

            // Act
            var result = await _controller.AddUser(newUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(newUser.FirstName, result.FirstName);
        }

        [TestMethod]
        public async Task GetAllUsers_ReturnsUserList()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), FirstName = "User 1", EmailAddress = "1@example.com" },
                new User { Id = Guid.NewGuid(), FirstName = "User 2", EmailAddress = "2@example.com" }
            };

            _mockUserRepository
                .Setup(repo => repo.GetAllUsersAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, ((List<User>)result).Count);
        }

        [TestMethod]
        public async Task UpdateUser_ReturnsUpdatedUser()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var updatedUser = new User { Id = Guid.Parse(userId), FirstName = "Updated", EmailAddress = "updated@example.com" };

            _mockUserRepository
                .Setup(repo => repo.UpdateUserAsync(userId, updatedUser))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateUser(userId, updatedUser);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updatedUser.FirstName, result.FirstName);
        }

        [TestMethod]
        public async Task DeleteUser_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();

            _mockUserRepository
                .Setup(repo => repo.DeleteUserAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Login_ReturnsTrue()
        {
            // Arrange
            var login = new Login { EmailAddress = "user@example.com", Password = "securePassword" };

            _mockUserRepository
                .Setup(repo => repo.Login(login))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Login(login);

            // Assert
            Assert.IsTrue(result);
        }
    }
}
