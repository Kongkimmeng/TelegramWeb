namespace Telegram_Web.Models
{
    public class EmpCard
    {
        public string? EmpID { get; set; }      // Employee ID from table
        public string? Name { get; set; }
        public string? TeamName { get; set; }
        public int Assign { get; set; }


         // Map VARBINARY column directly
        private byte[] _imageBytes = Array.Empty<byte>();
        public byte[] Image
        {
            get => _imageBytes;
            set
            {
                _imageBytes = value ?? Array.Empty<byte>();
                ImageDataUrl = ConvertBytesToDataUrl(_imageBytes);
            }
        }

        // Auto-generated Base64 string for <img> src
        public string ImageDataUrl { get; private set; } = "";

        private static string ConvertBytesToDataUrl(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            string base64 = Convert.ToBase64String(bytes);
            return $"data:image/jpeg;base64,{base64}";
        }
    }
}
