﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Scv.Db.Models.Auth
{
    public class RequestFileAccess
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string FileId { get; set; }
        public DateTimeOffset Requested { get; set; }
        public DateTimeOffset Expires { get; set; }
    }
}
