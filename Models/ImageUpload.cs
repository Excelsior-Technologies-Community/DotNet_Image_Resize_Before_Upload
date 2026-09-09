namespace DotNet_Image_Resize_Before_Upload.Models
{
    public class ImageUpload
    {
        public int ImageId { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string SavedFileName { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public int OriginalWidth { get; set; }

        public int OriginalHeight { get; set; }

        public int ResizedWidth { get; set; }

        public int ResizedHeight { get; set; }

        public long FileSize { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}