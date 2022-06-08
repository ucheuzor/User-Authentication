using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ClassLibrary.Model.Models
{
    public class Profile
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [StringLength(50)]
        public string Address1 { get; set; }

        [StringLength(50)]
        public string Address2 { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(100)]
        public string Landmark { get; set; }

        [StringLength(10)]
        public string Pin { get; set; }

        [StringLength(5)]
        public string CountryCode { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }
    }
}
