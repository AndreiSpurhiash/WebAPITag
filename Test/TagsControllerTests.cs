using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APITag.Controllers;
using APITag.Models;
using Microsoft.AspNetCore.Mvc;

namespace TagControllerTests
{
    [TestFixture]
    public class GetAllTagsTests
    {
        private TagController _tagController;
        private TagContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TagContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _dbContext = new TagContext(options);
            _tagController = new TagController(_dbContext, null, null);

            _dbContext.Tags.AddRange(new List<Tag>
            {
                new Tag { Id = 1, name = "Tag1", count = 10 },
                new Tag { Id = 2, name = "Tag2", count = 20 }
            });
            _dbContext.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetAllTags_ReturnsOkResult()
        {
            // Arrange

            // Act
            var result = await _tagController.GetAllTags();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);

            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as dynamic;

            Assert.AreEqual(30, response.count);
            Assert.AreEqual(1, response.Page);
            Assert.AreEqual(10, response.PageSize);

            var tags = response.Tags as List<Tag>;
            Assert.AreEqual(2, tags.Count);
            Assert.AreEqual(1, tags[0].Id);
            Assert.AreEqual("Tag1", tags[0].name);
            Assert.AreEqual(10, tags[0].count);
            Assert.AreEqual(2, tags[1].Id);
            Assert.AreEqual("Tag2", tags[1].name);
            Assert.AreEqual(20, tags[1].count);
        }
    }
}
