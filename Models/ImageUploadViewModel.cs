using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DotNet_Image_Resize_Before_Upload.Models
{
    public class ImageUploadViewModel
    {
        public int ImageId { get; set; }

        public IFormFile? ImageFile { get; set; }

        [Required]
        [Range(1, 5000)]
        public int Width { get; set; }

        [Required]
        [Range(1, 5000)]
        public int Height { get; set; }

        public bool MaintainAspectRatio { get; set; } = true;

        public string? ExistingImagePath { get; set; }

        public string? ExistingFileName { get; set; }
    }
}