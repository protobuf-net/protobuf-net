using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ProtoBuf.Models {
    public class DecodeViewModel {
        [Required]
        public string Content { get; set; }
        public bool Recursive { get; set; }

        //taken from https://stackoverflow.com/questions/6309379/how-to-check-for-a-valid-base64-encoded-string
        public bool IsBase64String () {
            if (string.IsNullOrEmpty (model.Content)) {
                return false;
            }
            var s = model.Content.Trim ();
            return (s.Length % 4 == 0) && Regex.IsMatch (s, "^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
        }

        public bool IsHexa () {
            if (string.IsNullOrEmpty (model.Content)) {
                return false;
            }
            //https://stackoverflow.com/questions/223832/check-a-string-to-see-if-all-characters-are-hexadecimal-values
            return Regex.IsMatch (model.Content, "\A\b[0-9a-fA-F]+\b\Z");
        }

        public byte[] GetData () {
            byte[] data = null;
            Content = Content.Replace (" ", "").Replace ("-", "");
            if (IsBase64String ()) {
                data = Convert.FromBase64String (Content);
            } else if (IsHexa ()) {
                int len = Content.Length / 2;
                var tmp = new byte[len];
                for (int i = 0; i < len; i++) {
                    tmp[i] = Convert.ToByte (Content.Substring (i * 2, 2), 16);
                }
                data = tmp;
            }
        }
    }
}