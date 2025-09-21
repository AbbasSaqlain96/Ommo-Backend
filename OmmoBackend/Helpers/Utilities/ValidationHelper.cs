namespace OmmoBackend.Helpers.Utilities
{
    public static class ValidationHelper
    {
        private static readonly string[] AllowedImageFormats = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedDocumentFormats = { ".pdf", ".docx", ".doc" };

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\+?[0-9]{10,15}$");
        }

        public static bool IsValidDocumentFormat(IFormFile file, string[] allowedExtensions)
        {
            if (file == null)
                return false;

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension);
        }

        public static bool IsValidImageFormat(IFormFile file, string[] allowedExtensions)
        {
            if (file == null)
                return false;

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension);
        }

        public static bool AreValidImageFormats(IEnumerable<IFormFile> files)
        {
            return files.All(file => AllowedImageFormats.Contains(Path.GetExtension(file.FileName).ToLower()));
        }

        public static bool AreValidDocumentFormats(IEnumerable<IFormFile> files)
        {
            return files.All(file => AllowedDocumentFormats.Contains(Path.GetExtension(file.FileName).ToLower()));
        }
    }
}
