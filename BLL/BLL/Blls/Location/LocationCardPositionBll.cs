﻿using DAL;
using DbModel.Location.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Blls.Location
{
    public class LocationCardPositionBll : BaseBll<LocationCardPosition, LocationDb>
    {
        public LocationCardPositionBll():base()
        {

        }
        public LocationCardPositionBll(LocationDb db) : base(db)
        {

        }

        protected override void InitDbSet()
        {
            DbSet = Db.LocationCardPositions;
        }

        public LocationCardPosition FindByCode(string code)
        {
            return DbSet.Find(code);
        }
    }
}