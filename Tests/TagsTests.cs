using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TagControllerTests
{
    [TestFixture]
    public class GetAllTagsTests
    {
        private TagController _tagController;
        private Mock<ILogger<TagController>> _loggerMock;
        private Mock<DbContext> _dbContextMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<TagController>>();
            _dbContextMock = new Mock<DbContext>();
            _tagController = new TagController(_loggerMock.Object, _dbContextMock.Object);
        }

        [Test]
        public async Task GetAllTags_ReturnsOkResult()
        {
            // Arrange
            var expectedTags = new List<TagDto>
            {
                new TagDto { id = 1, name = "Tag1", count = 10, percentage = 50 },
                new TagDto { id = 2, name = "Tag2", count = 20, percentage = 100 }
            };

            _dbContextMock.Setup(db => db.Tags).Returns(GetMockTagsDbSet().Object);
            _dbContextMock.Setup(db => db.Tags.SumAsync(tag => tag.count)).ReturnsAsync(30);
            _dbContextMock.Setup(db => db.Tags.AsQueryable()).Returns(GetMockTagsDbSet().Object);

            // Act
            var result = await _tagController.GetAllTags();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as dynamic;

            Assert.AreEqual(30, response.TotalCount);
            Assert.AreEqual(1, response.Page);
            Assert.AreEqual(10, response.PageSize);

            var tags = response.Tags as List<TagDto>;
            Assert.AreEqual(expectedTags.Count, tags.Count);

            for (int i = 0; i < expectedTags.Count; i++)
            {
                Assert.AreEqual(expectedTags[i].id, tags[i].id);
                Assert.AreEqual(expectedTags[i].name, tags[i].name);
                Assert.AreEqual(expectedTags[i].count, tags[i].count);
                Assert.AreEqual(expectedTags[i].percentage, tags[i].percentage);
            }
        }

        [Test]
        public async Task GetAllTags_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _dbContextMock.Setup(db => db.Tags).Throws(new Exception());

            // Act
            var result = await _tagController.GetAllTags();

            // Assert
            Assert.IsInstanceOf<StatusCodeResult>(result.Result);

            var statusCodeResult = result.Result as StatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.InternalServerError, statusCodeResult.StatusCode);
        }

        private Mock<DbSet<Tag>> GetMockTagsDbSet()
        {
            var tags = new List<Tag>
            {
                new Tag { Id = 1, name = "Tag1", count = 10 },
                new Tag { Id = 2, name = "Tag2", count = 20 }
            }.AsQueryable();

            var mockDbSet = new Mock<DbSet<Tag>>();
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Provider).Returns(tags.Provider);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.Expression).Returns(tags.Expression);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.ElementType).Returns(tags.ElementType);
            mockDbSet.As<IQueryable<Tag>>().Setup(m => m.GetEnumerator()).Returns(tags.GetEnumerator());

            return mockDbSet;
        }
    }
}