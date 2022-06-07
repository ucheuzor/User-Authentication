using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary.Model.Models
{
    public class Profile
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Landmark { get; set; }
        public string Pin { get; set; }
    }
}
