using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace albums_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnsecuredController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _allowedBasePath;
        private readonly ILogger<UnsecuredController> _logger;

        public UnsecuredController(IConfiguration configuration, IWebHostEnvironment hostEnvironment, ILogger<UnsecuredController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var configuredBasePath = configuration["FileAccess:BasePath"];
            var basePath = string.IsNullOrWhiteSpace(configuredBasePath)
                ? hostEnvironment.ContentRootPath
                : Path.Combine(hostEnvironment.ContentRootPath, configuredBasePath);
            _allowedBasePath = Path.GetFullPath(basePath);
            _logger = logger;
        }

        [HttpGet("health")]
        public ActionResult<string> Health()
        {
            _logger.LogDebug("UnsecuredController health endpoint called.");
            return Ok("ok");
        }

        [NonAction]
        public string? ReadFile(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return null;
            }

            var fullPath = Path.GetFullPath(Path.Combine(_allowedBasePath, userInput));

            if (!IsPathWithinBasePath(fullPath, _allowedBasePath))
            {
                throw new UnauthorizedAccessException("Requested file path is not allowed.");
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return null;
            }

            return System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
        }

        private static bool IsPathWithinBasePath(string fullPath, string basePath)
        {
            var relativePath = Path.GetRelativePath(basePath, fullPath);

            var traversesUp = relativePath == ".."
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

            return !Path.IsPathRooted(relativePath)
                && !traversesUp;
        }

        [NonAction]
        public int? GetProduct(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new ArgumentException("Product name is required.", nameof(productName));
            }

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Connection string is not configured.");
            }

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand sqlCommand = new SqlCommand(
                "SELECT TOP(1) ProductId FROM Products WHERE ProductName = @productName",
                connection
            )
            {
                CommandType = CommandType.Text,
            };

            sqlCommand.Parameters.Add("@productName", SqlDbType.NVarChar, 255).Value = productName;

            connection.Open();

            var result = sqlCommand.ExecuteScalar();
            if (result is null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }

        [NonAction]
        public string GetObject()
        {
            object o = new object();
            return o.ToString() ?? string.Empty;
        }
    }
}