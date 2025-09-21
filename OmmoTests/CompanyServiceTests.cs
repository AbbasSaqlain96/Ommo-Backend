using FluentAssertions.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.Http;

namespace OmmoTests
{
    public class CompanyServiceTests
    {
        private readonly Mock<ICompanyRepository> _companyRepoMock = new();
        private readonly Mock<ICarrierRepository> _carrierRepoMock = new();
        private readonly Mock<IDispatchServiceRepository> _dispatchRepoMock = new();
        private readonly Mock<ILogger<CompanyService>> _loggerMock = new();
        private readonly CompanyService _companyService;

        public CompanyServiceTests()
        {
            _companyService = new CompanyService(
                _companyRepoMock.Object,
                _carrierRepoMock.Object,
                _dispatchRepoMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenNameIsMissing()
        {
            var request = new CreateCompanyRequest { Name = "" };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Company name is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenNoContactMethod()
        {
            var request = new CreateCompanyRequest { Name = "Test", Email = "", Phone = "" };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("At least one contact method (email or phone number) is required.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenInvalidEmail()
        {
            var request = new CreateCompanyRequest { Name = "Test", Email = "invalidemail", Phone = "" };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Invalid email address format.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenInvalidPhone()
        {
            var request = new CreateCompanyRequest { Name = "Test", Email = "", Phone = "123abc" };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Invalid phone number format.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenInvalidCompanyType()
        {
            var request = new CreateCompanyRequest { Name = "Test", Email = "a@test.com", CompanyType = 3 };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Invalid company type. Must be 1 (Carrier) or 2 (Dispatcher).", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenInvalidCategoryType()
        {
            var request = new CreateCompanyRequest { Name = "Test", Email = "a@test.com", CompanyType = 1, CategoryType = 5 };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Invalid category type. Must be 1 (Independent), 2 (Parent), or 3 (Subsidized).", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenMCNumberMissingForCarrier()
        {
            var request = new CreateCompanyRequest
            {
                Name = "Test",
                CompanyType = 1,  // Carrier
                CategoryType = 1,
                Email = "carrier@test.com" // Required for contact validation to pass
            };

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("MC number is required for carrier companies.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenMCNumberDuplicate()
        {
            var request = new CreateCompanyRequest { Name = "Test", CompanyType = 1, CategoryType = 1, MCNumber = "MC123", Email = "carrier@test.com" };

            _companyRepoMock.Setup(r => r.CheckDuplicateMCNumberAsync("MC123", 1)).ReturnsAsync(true);

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("MC number already exists for another carrier", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenParentCompanyNotFound()
        {
            var request = new CreateCompanyRequest { Name = "Test", CompanyType = 2, CategoryType = 1, ParentId = 99, Email = "carrier@test.com" };

            _companyRepoMock.Setup(r => r.CheckCompanyExistsAsync(99)).ReturnsAsync(false);

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Parent company not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Name = "Test",
                CompanyType = 1,          // Carrier
                CategoryType = 1,         // Independent
                MCNumber = "MC123",
                Email = "carrier@test.com",
                ParentId = 0              // No parent, which skips parent check
            };

            var companyResult = new CompanyCreationResult
            {
                CompanyId = 1,
                Success = true
            };

            _companyRepoMock
                .Setup(r => r.CheckDuplicateMCNumberAsync("MC123", 1))
                .ReturnsAsync(false);

            _companyRepoMock
                .Setup(r => r.CreateCompanyAsync(request))
                .ReturnsAsync(companyResult);

            // Act
            var result = await _companyService.CreateCompanyAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Company created successfully.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.CompanyId);
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnError_WhenExceptionThrown()
        {
            var request = new CreateCompanyRequest { Name = "Test", CompanyType = 1, CategoryType = 1, MCNumber = "MC123", Email = "carrier@test.com" };

            _companyRepoMock.Setup(r => r.CheckDuplicateMCNumberAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB error"));

            var result = await _companyService.CreateCompanyAsync(request);

            Assert.False(result.Success);
            Assert.Equal(503, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_ReturnsProfile_WhenFound()
        {
            var companyId = 1;
            var profile = new CompanyProfileDto { CompanyId = companyId, Name = "Test Co" };

            _companyRepoMock.Setup(r => r.GetCompanyProfileAsync(companyId)).ReturnsAsync(profile);

            var result = await _companyService.GetCompanyProfileAsync(companyId);

            Assert.True(result.Success);
            Assert.Equal("Company profile retrieved successfully.", result.Message);
            Assert.Equal(companyId, result.Data.CompanyId);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_ReturnsNotFound_WhenProfileIsNull()
        {
            var companyId = 1;

            _companyRepoMock.Setup(r => r.GetCompanyProfileAsync(companyId)).ReturnsAsync((CompanyProfileDto)null);

            var result = await _companyService.GetCompanyProfileAsync(companyId);

            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Equal("Company not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_ReturnsNotFound_WhenKeyNotFoundExceptionThrown()
        {
            var companyId = 1;

            _companyRepoMock.Setup(r => r.GetCompanyProfileAsync(companyId))
                .ThrowsAsync(new KeyNotFoundException("Company does not exist"));

            var result = await _companyService.GetCompanyProfileAsync(companyId);

            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Equal("Company does not exist", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCompanyProfileAsync_ReturnsServerError_OnUnexpectedException()
        {
            var companyId = 1;

            _companyRepoMock.Setup(r => r.GetCompanyProfileAsync(companyId))
                .ThrowsAsync(new Exception("DB down"));

            var result = await _companyService.GetCompanyProfileAsync(companyId);

            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
            Assert.Equal("Server is temporarily unavailable. Please try again later.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_CompanyNotFound_Returns404()
        {
            _companyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Company)null);

            var response = await _companyService.UpdateCompanyProfileAsync(1, new UpdateCompanyProfileDto());

            Assert.False(response.Success);
            Assert.Equal(404, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_InvalidEmail_Returns400()
        {
            _companyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Company());

            var dto = new UpdateCompanyProfileDto { Email = "invalid-email" };
            var response = await _companyService.UpdateCompanyProfileAsync(1, dto);

            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Invalid email address format.", response.ErrorMessage);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_InvalidPhone_Returns400()
        {
            _companyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Company());

            var dto = new UpdateCompanyProfileDto { Phone = "bad-phone" };
            var response = await _companyService.UpdateCompanyProfileAsync(1, dto);

            Assert.False(response.Success);
            Assert.Equal(400, response.StatusCode);
            Assert.Equal("Invalid phone number format.", response.ErrorMessage);
        }

        [Fact]
        public async Task UpdateCompanyProfileAsync_ValidUpdate_ReturnsSuccess()
        {
            var company = new Company();
            _companyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(company);
            _companyRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

            var dto = new UpdateCompanyProfileDto
            {
                Name = "Company89",
                Email = "test@company89.com",
                Phone = "+19687578574764",
                Address = "123 Street"
            };

            var response = await _companyService.UpdateCompanyProfileAsync(1, dto);

            Assert.True(response.Success);
            Assert.Equal("Company profile updated successfully.", response.Message);
        }
    }
}
