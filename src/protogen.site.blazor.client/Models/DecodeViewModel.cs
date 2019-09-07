using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProtoBuf.Models
{
    public class DecodeViewModel
    {
        private readonly IJSRuntime jSRuntime;

        public DecodeViewModel(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
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
        public ElementReference FileInput { get; set; }
        public bool Recursive { get; set; }
        public DecodeContentTypeEnum DecodeContentType { get; set; } = DecodeContentTypeEnum.Hexa;

        private async Task<byte[]> GetData()
        {

            switch (DecodeContentType)
            {
                case DecodeContentTypeEnum.Hexa:
                    Hexadecimal = Hexadecimal.Replace(" ", "").Replace("-", "").Trim();

                    int len = Hexadecimal.Length / 2;
                    Console.WriteLine("data.length = " + len);

                    var tmp = new byte[len];
                    for (int i = 0; i < len; i++)
                    {
                        tmp[i] = Convert.ToByte(Hexadecimal.Substring(i * 2, 2), 16);
                    }
                    return tmp;
                case DecodeContentTypeEnum.Base64:
                    return Convert.FromBase64String(Base64);
                case DecodeContentTypeEnum.File:
                    var content = await jSRuntime.InvokeAsync<string>("getFileContent", FileInput);
                    return Convert.FromBase64String(content);

                default:
                    throw new ArgumentOutOfRangeException($"Decode content type not implemented {DecodeContentType}");

            }
        }

        public async Task<DecodeModel> GetDecodeModel()
        {
            var data = await GetData();
            return (new DecodeModel(data, Recursive));
        }
    }
}