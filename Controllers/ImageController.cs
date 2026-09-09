using DotNet_Image_Resize_Before_Upload.Models;
using DotNet_Image_Resize_Before_Upload.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DotNet_Image_Resize_Before_Upload.Controllers
{
    public class ImageController : Controller
    {
        private readonly IImageResizeService _imageResizeService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public ImageController(
            IImageResizeService imageResizeService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _imageResizeService = imageResizeService;
            _configuration = configuration;
            _environment = environment;
        }

        // GET: Image
        public async Task<IActionResult> Index()
        {
            List<ImageUpload> images =
                new List<ImageUpload>();

            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_GetAll",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                images.Add(new ImageUpload
                {
                    ImageId =
                        Convert.ToInt32(
                            reader["ImageId"]),

                    OriginalFileName =
                        reader["OriginalFileName"]
                        .ToString() ?? "",

                    SavedFileName =
                        reader["SavedFileName"]
                        .ToString() ?? "",

                    ImagePath =
                        reader["ImagePath"]
                        .ToString() ?? "",

                    OriginalWidth =
                        Convert.ToInt32(
                            reader["OriginalWidth"]),

                    OriginalHeight =
                        Convert.ToInt32(
                            reader["OriginalHeight"]),

                    ResizedWidth =
                        Convert.ToInt32(
                            reader["ResizedWidth"]),

                    ResizedHeight =
                        Convert.ToInt32(
                            reader["ResizedHeight"]),

                    FileSize =
                        Convert.ToInt64(
                            reader["FileSize"]),

                    CreatedDate =
                        Convert.ToDateTime(
                            reader["CreatedDate"])
                });
            }

            return View(images);
        }

        // GET: Image/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Image/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            ImageUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.ImageFile == null)
            {
                ModelState.AddModelError(
                    "ImageFile",
                    "Please select an image.");

                return View(model);
            }

            try
            {
                ImageUpload? result =
                    await _imageResizeService
                        .ResizeAndSaveAsync(
                            model.ImageFile,
                            model.Width,
                            model.Height,
                            model.MaintainAspectRatio);

                if (result == null)
                {
                    TempData["Error"] =
                        "Image upload failed.";

                    return View(model);
                }

                TempData["Success"] =
                    "Image resized and uploaded successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);

                return View(model);
            }
        }

        // GET: Image/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id)
        {
            ImageUpload? image =
                await GetImageById(id);

            if (image == null)
            {
                TempData["Error"] =
                    "Image not found.";

                return RedirectToAction(nameof(Index));
            }

            string physicalPath =
                Path.Combine(
                    _environment.WebRootPath,
                    image.ImagePath.TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_Delete",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@ImageId",
                id);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            TempData["Success"] =
                "Image deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<ImageUpload?> GetImageById(
            int id)
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_GetById",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@ImageId",
                id);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ImageUpload
                {
                    ImageId =
                        Convert.ToInt32(
                            reader["ImageId"]),

                    OriginalFileName =
                        reader["OriginalFileName"]
                        .ToString() ?? "",

                    SavedFileName =
                        reader["SavedFileName"]
                        .ToString() ?? "",

                    ImagePath =
                        reader["ImagePath"]
                        .ToString() ?? "",

                    OriginalWidth =
                        Convert.ToInt32(
                            reader["OriginalWidth"]),

                    OriginalHeight =
                        Convert.ToInt32(
                            reader["OriginalHeight"]),

                    ResizedWidth =
                        Convert.ToInt32(
                            reader["ResizedWidth"]),

                    ResizedHeight =
                        Convert.ToInt32(
                            reader["ResizedHeight"]),

                    FileSize =
                        Convert.ToInt64(
                            reader["FileSize"]),

                    CreatedDate =
                        Convert.ToDateTime(
                            reader["CreatedDate"])
                };
            }

            return null;
        }
    }
}