using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.DTOs
{
    public class UpdateUserDto
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
