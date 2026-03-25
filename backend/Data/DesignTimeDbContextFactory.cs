using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MAFStudio.Backend.Data
{
    /// <summary>
    /// 设计时DbContext工厂，用于EF Core迁移
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql("Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
