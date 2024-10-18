using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ST10140587_Prog6212_Part2.Controllers;
using ST10140587_Prog6212_Part2.Data;
using ST10140587_Prog6212_Part2.Models;
using ProjectClaim = ST10140587_Prog6212_Part2.Models.Claim; // Alias to avoid conflict with System.Security.Claims.Claim
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace ST10140587_Prog6212_UnitTests
{
    public class ClaimsControllerTests
    {
        private ClaimsController CreateControllerWithMockUser(ApplicationDbContext dbContext, string userName = "ValidLecturer")
        {
            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.Identity.Name).Returns(userName);

            var controller = new ClaimsController(dbContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = mockUser.Object }
                }
            };

            return controller;
        }

        [Fact]
        public async Task SubmitClaim_ValidClaim_ReturnsRedirectToTrackClaims()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "SubmitClaimTestDB").Options;
            var dbContext = new ApplicationDbContext(options);

            var controller = CreateControllerWithMockUser(dbContext);

            var claim = new ProjectClaim
            {
                LecturerName = "ValidLecturer", // Required field
                HoursWorked = 8,
                HourlyRate = 50,
                Notes = "Valid Claim",
                DocumentPath = "/uploads/test.pdf", // Mimic the document path being set
                Status = "Pending" // Set status to avoid issues
            };

            // Create a mock document to pass the validation
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("test.pdf");
            fileMock.Setup(f => f.Length).Returns(1024); // 1 KB mock size
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));
            var document = fileMock.Object;

            // Act
            var result = await controller.SubmitClaim(claim, document);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("TrackClaims", redirectResult.ActionName);
        }


        [Fact]
        public async Task TrackClaims_InvalidLecturer_NoClaimsShown()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TrackClaimsTestDB").Options;
            var dbContext = new ApplicationDbContext(options);

            // Add a valid claim with all required properties
            var claim = new ProjectClaim
            {
                LecturerName = "Lecturer1",
                HoursWorked = 10,
                HourlyRate = 100,
                Notes = "Sample Notes", // Required property
                DocumentPath = "/uploads/sample.pdf", // Required property
                Status = "Pending" // Optional, but helpful to avoid logic errors
            };
            dbContext.Claims.Add(claim);
            await dbContext.SaveChangesAsync();

            var controller = CreateControllerWithMockUser(dbContext, "InvalidLecturer");

            // Act
            var result = await controller.TrackClaims();
            var viewResult = Assert.IsType<ViewResult>(result);
            var claims = Assert.IsAssignableFrom<IEnumerable<ProjectClaim>>(viewResult.Model);

            // Assert
            Assert.Empty(claims);
        }


        [Fact]
        public async Task ApproveClaim_NonExistentClaim_ShowsError()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ApproveClaimTestDB").Options;
            var dbContext = new ApplicationDbContext(options);
            var controller = CreateControllerWithMockUser(dbContext);

            // Act
            var result = await controller.ApproveClaim(99); // Non-existent claim ID

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ViewPendingClaims", redirectResult.ActionName);
            Assert.Contains("Claim not found.", controller.ModelState[string.Empty].Errors.FirstOrDefault()?.ErrorMessage);
        }

        [Fact]
        public async Task RejectClaim_ValidClaim_ChangesStatusToRejected()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "RejectClaimTestDB").Options;
            var dbContext = new ApplicationDbContext(options);

            var claim = new ProjectClaim
            {
                ClaimId = 1,
                LecturerName = "Lecturer1",
                Status = "Pending",
                DocumentPath = "/uploads/test.pdf",  // Providing required property
                Notes = "Test claim"                 // Providing required property
            };

            dbContext.Claims.Add(claim);
            await dbContext.SaveChangesAsync();

            var controller = CreateControllerWithMockUser(dbContext);

            // Act
            var result = await controller.RejectClaim(1); // Valid claim ID

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ViewPendingClaims", redirectResult.ActionName);

            var updatedClaim = await dbContext.Claims.FindAsync(1);
            Assert.Equal("Rejected", updatedClaim.Status);
        }
    }
}