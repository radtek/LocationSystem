﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbModel.Engine
{
    public class bus_tag
    {
        [Key]
        public int id { get; set; }

        public string tag_id { get; set; }

        public int tag_id_dec { get; set; }
    }
}
