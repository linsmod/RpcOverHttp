using RpcServiceCollection;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace RpcService.Implementation
{
    /// <summary>
    /// appdb
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// constructor
        /// </summary>
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }
        /// <summary>
        /// 匹配项分组
        /// </summary>
        public DbSet<MatchGroup> MatchGroups { get; set; }
    }
}