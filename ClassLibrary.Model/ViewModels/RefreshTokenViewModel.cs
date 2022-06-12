using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary.Model.ViewModels
{
    public class RefreshTokenViewModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
