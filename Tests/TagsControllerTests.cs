using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public class TagsControllerTests
    {
        private TagsController _tagsController;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<ILogger<TagsController>> _loggerMock;
        private Mock<HttpClient> _httpClientMock;
        private Mock<HttpContent> _httpContentMock;
        private Mock<HttpResponseMessage> _httpResponseMock;
        private Mock<Stream> _streamMock;
        private Mock<GZipStream> _gzipStreamMock;
        private Mock<StreamReader> _streamReaderMock;
        private Mock<DbSet<Tag>> _dbSetMock;
        private Mock<DbContext> _dbContextMock;

        [SetUp]
        public void Setup()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<TagsController>>();
            _httpClientMock = new Mock<HttpClient>();
            _httpContentMock = new Mock<HttpContent>();
            _httpResponseMock = new Mock<HttpResponseMessage>();
            _streamMock = new Mock<Stream>();
            _gzipStreamMock = new Mock<GZipStream>();
            _streamReaderMock = new Mock<StreamReader>();
            _dbSetMock = new Mock<DbSet<Tag>>();
            _dbContextMock = new Mock<DbContext>();

            _tagsController = new TagsController(_httpClientFactoryMock.Object, _loggerMock.Object, _dbContextMock.Object);
        }

        [Test]
        public async Task UpdateTags_ReturnsOkResult_WhenTagsUpdatedSuccessfully()
        {
            // Arrange
            var responseContent = "Response Content";
            var stackExchangeResponse = new StackExchangeResponse { items = new List<Tag>() };
            var json = JsonSerializer.Serialize(stackExchangeResponse);

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);
            _httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(_httpResponseMock.Object);
            _httpResponseMock.Setup(response => response.IsSuccessStatusCode).Returns(true);
            _httpResponseMock.Setup(response => response.Content).Returns(_httpContentMock.Object);
            _httpContentMock.Setup(content => content.ReadAsStreamAsync()).ReturnsAsync(_streamMock.Object);
            _streamMock.Setup(stream => stream.CanRead).Returns(true);
            _streamMock.Setup(stream => stream.CanWrite).Returns(true);
            _gzipStreamMock.Setup(gzipStream => gzipStream.CanRead).Returns(true);
            _gzipStreamMock.Setup(gzipStream => gzipStream.CanWrite).Returns(true);
            _streamReaderMock.Setup(reader => reader.ReadToEndAsync()).ReturnsAsync(json);
            _dbContextMock.Setup(context => context.Tags).Returns(_dbSetMock.Object);
            _dbSetMock.Setup(dbSet => dbSet.AddRange(It.IsAny<IEnumerable<Tag>>()));
            _dbContextMock.Setup(context => context.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _tagsController.UpdateTags();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            Assert.AreEqual("Tags updated successfully", (result as OkObjectResult).Value);
        }

        [Test]
        public async Task UpdateTags_ReturnsStatusCodeResult_WhenFailedToFetchTags()
        {
            // Arrange
            var statusCode = HttpStatusCode.BadRequest;
            var responseContent = "Failed to fetch tags from StackExchange API. Status code: " + statusCode;

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);
            _httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(_httpResponseMock.Object);
            _httpResponseMock.Setup(response => response.IsSuccessStatusCode).Returns(false);
            _httpResponseMock.Setup(response => response.StatusCode).Returns(statusCode);

            // Act
            var result = await _tagsController.UpdateTags();

            // Assert
            Assert.IsInstanceOf<StatusCodeResult>(result);
            Assert.AreEqual((int)statusCode, (result as StatusCodeResult).StatusCode);
            Assert.AreEqual(responseContent, (result as StatusCodeResult).StatusDescription);
        }

        [Test]
        public async Task UpdateTags_ReturnsStatusCodeResult_WhenJsonDeserializationFails()
        {
            // Arrange
            var exceptionMessage = "Json deserialization error";
            var responseContent = $"Deserialization ERROR JSON: {exceptionMessage}";

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(_httpClientMock.Object);
            _httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>())).ReturnsAsync(_httpResponseMock.Object);
            _httpResponseMock.Setup(response => response.IsSuccessStatusCode).Returns(true);
            _httpResponseMock.Setup(response => response.Content).Returns(_httpContentMock.Object);
            _httpContentMock.Setup(content => content.ReadAsStreamAsync()).ReturnsAsync(_streamMock.Object);
            _streamMock.Setup(stream => stream.CanRead).Returns(true);
            _streamMock.Setup(stream => stream.CanWrite).Returns(true);
            _gzipStreamMock.Setup(gzipStream => gzipStream.CanRead).Returns(true);
            _gzipStreamMock.Setup(gzipStream => gzipStream.CanWrite).Returns(true);
            _streamReaderMock.Setup(reader => reader.ReadToEndAsync()).ThrowsAsync(new JsonException(exceptionMessage));

            // Act
            var result = await _tagsController.UpdateTags();

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            Assert.AreEqual(responseContent, (result as OkObjectResult).Value);
        }

        [Test]
        public async Task UpdateTags_ReturnsStatusCodeResult_WhenExceptionOccurs()
        {
            // Arrange
            var exceptionMessage = "An error occurred while processing your request.";
            var statusCode = HttpStatusCode.InternalServerError;

            _httpClientFactoryMock.Setup(factory => factory.CreateClient(It.IsAny<string>())).Throws(new Exception(exceptionMessage));

            // Act
            var result = await _tagsController.UpdateTags();

            // Assert
            Assert.IsInstanceOf<StatusCodeResult>(result);
            Assert.AreEqual((int)statusCode, (result as StatusCodeResult).StatusCode);
            Assert.AreEqual(exceptionMessage, (result as StatusCodeResult).StatusDescription);
        }
    }
}