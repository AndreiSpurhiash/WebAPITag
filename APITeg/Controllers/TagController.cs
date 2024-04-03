
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using APITag.Models;


#nullable enable
namespace APITag.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly TagContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TagController> _logger;

        public TagController(TagContext context, IHttpClientFactory httpClientFactory, ILogger<TagController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        [HttpGet("GetTagsFromDB")]
        [SwaggerOperation(Summary = "Get a list of tags with pagination and sorting")]
        [SwaggerResponse(200, "Successful operation", typeof(IEnumerable<TagDto>))]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetAllTags(int page = 1, int pageSize = 10, string sortBy = "Name", string sortOrder = "asc")
        {

            try
            {
                var query = _context.Tags.AsQueryable();

                switch (sortBy.ToLower())
                {
                    case "name":
                        query = sortOrder.ToLower() == "asc" ? query.OrderBy(tag => tag.name) : query.OrderByDescending(tag => tag.name);
                        break;
                    case "percentage":
                        query = sortOrder.ToLower() == "asc" ? query.OrderBy(tag => tag.percentage) : query.OrderByDescending(tag => tag.percentage);
                        break;
                    default:
                        query = query.OrderBy(tag => tag.name);
                        break;
                }

                var totalTagCount = await _context.Tags.SumAsync(tag => tag.count);

                query = query.Skip((page - 1) * pageSize).Take(pageSize);

                var tagDtos = await query.Select(tag => new TagDto
                {
                    id = tag.Id,
                    name = tag.name,
                    count = tag.count,
                    percentage = totalTagCount != 0 ? (double)tag.count / totalTagCount * 100 : 0
                }).ToListAsync();

                return Ok(new
                {
                    TotalCount = totalTagCount,
                    Page = page,
                    PageSize = pageSize,
                    Tags = tagDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing GetAllTags request.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An error occurred while processing your request.");
            }
            
        }

        [HttpGet("GetTagsFromAPI")]
        [SwaggerOperation(Summary = "Update tags from StackExchange API")]
        [SwaggerResponse(200, "Successful operation")]
        public async Task<IActionResult> UpdateTags()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                Random random = new Random();

                while (_context.Tags.Count() < 1000)
                {
                    int min = 1000;
                    int max = 250000;
                    int randomNumber = random.Next(min, max + 1);

                    var response = await client.GetAsync($"https://api.stackexchange.com/2.3/tags?pagesize=100&order=desc&max={randomNumber}&sort=popular&site=stackoverflow");

                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, $"Failed to fetch tags from StackExchange API. Status code: {response.StatusCode}");
                    }

                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    using var gzipStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    using var streamReader = new StreamReader(gzipStream);
                    var json = await streamReader.ReadToEndAsync();

                    try
                    {
                        var tagsResponse = JsonSerializer.Deserialize<StackExchangeResponse>(json);
                        _context.Tags.AddRange(tagsResponse.items);
                        await _context.SaveChangesAsync();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Ошибка десериализации JSON: {ex.Message}");
                    }
                }


                return Ok("Tags updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing UpdateTags request.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An error occurred while processing your request."); 
            }
            
        }
    }
}

