using DUTClinicManagement.Data;
using DUTClinicManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

public class DUTClinicManagementDbContextFactory : IDesignTimeDbContextFactory<DUTClinicManagementDbContext>
{
    public DUTClinicManagementDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<DUTClinicManagementDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DUTClinicManagementDbContext(optionsBuilder.Options);
    }
}
