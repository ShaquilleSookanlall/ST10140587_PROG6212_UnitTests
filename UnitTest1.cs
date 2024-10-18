using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST10140587_Prog6212_Part2.Controllers;
using ST10140587_Prog6212_Part2.Data;
using CustomClaim = ST10140587_Prog6212_Part2.Models.Claim;
using ST10140587_Prog6212_Part2.Models;
using Moq;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ST10140587_Prog6212_Part2;

namespace ST10140587_Prog6212_UnitTests
{
    public class ClaimsControllerTests
    {




        [Fact]
        public async Task TrackClaims_InvalidLecturer_NoClaimsShown()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                          .UseInMemoryDatabase(databaseName: "InvalidLecturerTest").Options;
            var dbContext = new ApplicationDbContext(options);

            dbContext.Claims.Add(new ST10140587_Prog6212_Part2.Models.Claim
            {
                LecturerName = "Lecturer1",
                HoursWorked = 10,
                Notes = "Test Note",
                DocumentPath = "/path/to/document.pdf"
            });

            await dbContext.SaveChangesAsync();

            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.Identity.Name).Returns("InvalidLecturer");

            var controller = new ClaimsController(dbContext)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = mockUser.Object }
                }
            };

            // Act
            var result = await controller.TrackClaims();
            var viewResult = Assert.IsType<ViewResult>(result);
            var claims = Assert.IsAssignableFrom<IEnumerable<ST10140587_Prog6212_Part2.Models.Claim>>(viewResult.Model);

            // Assert
            Assert.Empty(claims);
        }


        [Fact]
        public async Task ApproveClaim_NonExistentClaim_ShowsError()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                          .UseInMemoryDatabase(databaseName: "ApproveNonExistentClaimTest").Options;
            var dbContext = new ApplicationDbContext(options);
            var controller = new ClaimsController(dbContext);

            // Act
            var result = await controller.ApproveClaim(99);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ViewPendingClaims", redirectResult.ActionName);

            Assert.Contains("Claim not found.", controller.ModelState[string.Empty].Errors.FirstOrDefault()?.ErrorMessage);
        }
    }
}
