using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ProtoBuf.Models
{
    public class DecodeViewModel
    {
        [Required]
        public string Content { get; set; }
        public bool Recursive { get; set; }
    }
}
