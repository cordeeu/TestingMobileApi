﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using MobileApi.Models;
using System.Threading.Tasks;

namespace MobileApi.Models
{
    public class Repository<T> where T : class
    {
        internal MobileApiContext mobileApiContext = new MobileApiContext();
        internal DbSet<T> DbSet;

        public Repository()
        {
            DbSet = mobileApiContext.Set<T>();
        }

        public IEnumerable<T> GetAll()
        {
            IEnumerable<T> query = DbSet;
            if (query.Any())
            {
                return query.ToList();
            }
            return null;
        }

        public T GetById(int id)
        {
            return DbSet.Find(id);
        }

        public IEnumerable<PumaImage> GetImages(int resourceId)
        {
            IEnumerable<PumaImage> query = mobileApiContext.PumaImages.Where(x => x.PumaId == resourceId);
            if (query.Any())
            {
                return query.ToList();
            }
            return null;
        }

        public async Task<PumaImage> GetImage(int imageId)
        {
            return await mobileApiContext.PumaImages.FindAsync(imageId);
        }

    }
}