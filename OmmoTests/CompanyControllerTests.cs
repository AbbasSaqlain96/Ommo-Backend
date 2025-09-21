using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using OmmoBackend.Controllers;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmmoTests
{
    public class CompanyControllerTests
    {
        private readonly Mock<ICompanyService> _companyServiceMock = new();
        private readonly Mock<IUserService> _userServiceMock = new();
        private readonly Mock<ILogger<CompanyController>> _loggerMock = new();
        private readonly CompanyController _controller;

        public CompanyControllerTests()
        {
            _controller = new CompanyController(_companyServiceMock.Object, _userServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateCompany_InvalidModel_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Name", "Required");

            var result = await _controller.CreateCompany(new CreateCompanyRequest());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);

            var json = JsonSerializer.Serialize(objectResult.Value);
            Console.WriteLine("Returned JSON: " + json);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("errorMessage", out var errorElement), "The 'errorMessage' property is missing in the response.");
            var errorMessage = errorElement.GetString();

            Assert.Equal("Required", errorMessage);
        }

        [Fact]
        public async Task CreateCompany_ServiceReturnsError_ReturnsErrorResponse()
        {
            var request = new CreateCompanyRequest { Name = "Test" };

            _companyServiceMock.Setup(s => s.CreateCompanyAsync(request))
                .ReturnsAsync(ServiceResponse<CompanyCreationResult>.ErrorResponse("Error occurred", 400));

            var result = await _controller.CreateCompany(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateCompany_Success_ReturnsCompanyId()
        {
            var request = new CreateCompanyRequest { Name = "Valid Company" };

            _companyServiceMock.Setup(s => s.CreateCompanyAsync(request))
                .ReturnsAsync(ServiceResponse<CompanyCreationResult>.SuccessResponse(
                    new CompanyCreationResult { CompanyId = 123 }, "Success"));

            var result = await _controller.CreateCompany(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var json = JsonSerializer.Serialize(result.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = root.GetProperty("data");
            var companyId = data.GetProperty("companyId").GetInt32();

            var message = root.GetProperty("message").GetString();

            Assert.Equal(123, companyId);
            Assert.Equal("Success", message);
        }

        [Fact]
        public async Task CreateCompany_ExceptionThrown_ReturnsServerError()
        {
            var request = new CreateCompanyRequest { Name = "Oops" };

            _companyServiceMock.Setup(s => s.CreateCompanyAsync(request)).ThrowsAsync(new System.Exception("Something failed"));

            var result = await _controller.CreateCompany(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(503, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyProfile_MissingCompanyIdClaim_ReturnsUnauthorized()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No claims
            };

            var result = await _controller.GetCompanyProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyProfile_InvalidCompanyIdFormat_ReturnsUnauthorized()
        {
            SetUserClaims("notanumber");

            var result = await _controller.GetCompanyProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        //[Fact]
        //public async Task GetCompanyProfile_Success_ReturnsProfile()
        //{
        //    // Arrange
        //    var companyId = 1;
        //    SetUserClaims(companyId.ToString());

        //    var profile = new CompanyProfileDto { CompanyId = companyId, Name = "Test Co" };
        //    var serviceResponse = ServiceResponse<CompanyProfileDto>.SuccessResponse(profile, "OK");

        //    _companyServiceMock.Setup(s => s.GetCompanyProfileAsync(companyId)).ReturnsAsync(serviceResponse);

        //    // Act
        //    var result = await _controller.GetCompanyProfile() as ObjectResult;

        //    // Assert
        //    Assert.NotNull(result);  // Ensure that result is not null
        //    Assert.Equal(200, result.StatusCode);  // Assert that status code is 200 (OK)

        //    // Assert that the response value contains the expected data
        //    dynamic value = result.Value;
        //    Assert.NotNull(value);
        //    Assert.Equal("OK", value.message.ToString());
        //    Assert.NotNull(value.data);
        //    Assert.Equal(companyId, value.data.companyId);
        //    Assert.Equal("Test Co", value.data.name.ToString());
        //}

        [Fact]
        public async Task GetCompanyProfile_NotFound_ReturnsError()
        {
            var companyId = 2;
            SetUserClaims(companyId.ToString());

            _companyServiceMock.Setup(s => s.GetCompanyProfileAsync(companyId))
                .ReturnsAsync(ServiceResponse<CompanyProfileDto>.ErrorResponse("Not found", StatusCodes.Status404NotFound));

            var result = await _controller.GetCompanyProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetCompanyProfile_ServiceThrowsException_ReturnsServerError()
        {
            var companyId = 3;
            SetUserClaims(companyId.ToString());

            _companyServiceMock.Setup(s => s.GetCompanyProfileAsync(companyId)).ThrowsAsync(new System.Exception("Server crashed"));

            var result = await _controller.GetCompanyProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(503, result.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfile_InvalidModel_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Name", "Required");

            var result = await _controller.UpdateCompanyProfile(new UpdateCompanyProfileDto());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfile_NullBody_ReturnsBadRequest()
        {
            var result = await _controller.UpdateCompanyProfile(null);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfile_InvalidCompanyId_ReturnsBadRequest()
        {
            // Arrange
            // Simulate invalid company ID from token
            SetUserClaims("-1");
            var updateDto = new UpdateCompanyProfileDto();

            // Act
            var result = await _controller.UpdateCompanyProfile(updateDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfile_ServiceFails_ReturnsError()
        {
            SetUserClaims("123");

            _companyServiceMock.Setup(s => s.UpdateCompanyProfileAsync(123, It.IsAny<UpdateCompanyProfileDto>()))
                .ReturnsAsync(ServiceResponse<CompanyProfileDto>.ErrorResponse("Failed", 500));

            var result = await _controller.UpdateCompanyProfile(new UpdateCompanyProfileDto());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCompanyProfile_Success_ReturnsOk()
        {
            SetUserClaims("123");

            var dto = new UpdateCompanyProfileDto { Name = "Valid Name" };
            _companyServiceMock.Setup(s => s.UpdateCompanyProfileAsync(123, dto))
                .ReturnsAsync(ServiceResponse<CompanyProfileDto>.SuccessResponse(null, "Company profile updated successfully."));

            var result = await _controller.UpdateCompanyProfile(dto);

            var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(200, objectResult.StatusCode);

            var json = JsonSerializer.Serialize(objectResult.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("message", out _));
            Assert.Equal("Company profile updated successfully.", doc.RootElement.GetProperty("message").GetString());

        }

        private void SetUserClaims(string companyIdValue)
        {
            var claims = new List<Claim>
            {
                new Claim("Company_ID", companyIdValue)
            };
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
