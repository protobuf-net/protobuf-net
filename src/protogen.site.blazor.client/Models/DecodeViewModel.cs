using BlazorInputFile;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ProtoBuf.Models
{
    public class DecodeViewModel
    {

        public DecodeViewModel()
        {
        }
        public enum DecodeContentTypeEnum
        {
            Hexa,
            Base64,
            File
        }
        [RegularExpression(@"\A\b[0-9a-fA-F\s]+\b\Z")]
        public string Hexadecimal { get; set; }
        [RegularExpression(@"^[a-zA-Z0-9\+/]*={0,3}$")]
        public string Base64 { get; set; }
        public IFileListEntry File { get; set; }
        public DecodeContentTypeEnum DecodeContentType { get; set; } = DecodeContentTypeEnum.File;

        private async Task<byte[]> GetData()
        {

            switch (DecodeContentType)
            {
                case DecodeContentTypeEnum.Hexa:
                    Hexadecimal = Hexadecimal.Replace(" ", "").Replace("-", "").Trim();

                    int len = Hexadecimal.Length / 2;

                    var tmp = new byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        tmp[i] = Convert.ToByte(Hexadecimal.Substring(i * 2, 2), 16);
                    }
                    return tmp;
                case DecodeContentTypeEnum.Base64:
                    return Convert.FromBase64String(Base64);
                case DecodeContentTypeEnum.File:
                    var ms = new System.IO.MemoryStream();
                    await File.Data.CopyToAsync(ms);
                    return ms.ToArray();

                default:
                    throw new ArgumentOutOfRangeException($"Decode content type not implemented {DecodeContentType}");

            }
        }

        public async Task<DecodeModel> GetDecodeModel()
        {
            var data = await GetData();
            return (new DecodeModel(data));
        }
    }
}