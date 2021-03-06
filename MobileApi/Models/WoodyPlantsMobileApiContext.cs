﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MobileApi.Models
{
    public class WoodyPlantsMobileApiContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
    
        public WoodyPlantsMobileApiContext() : base("name=MobileApiContext")
        {
            this.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public DbSet<WoodyPlant> Plants { get; set; }
        public DbSet<ImageWoody> Images { get; set; }
        public DbSet<WoodySetting> WoodySettings { get; set; }
    }
}
