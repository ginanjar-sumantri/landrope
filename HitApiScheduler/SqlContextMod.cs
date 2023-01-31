using HitApiScheduler.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitApiScheduler;
public class SqlContextMod : DbContext
{
    public SqlContextMod()
    {
    }

    public SqlContextMod(DbContextOptions<SqlContextMod> options)
        : base(options)
    {
    }
    public DbSet<EmailList> EmailList { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Default");
        optionsBuilder.UseSqlServer(connectionString);
    }
}
