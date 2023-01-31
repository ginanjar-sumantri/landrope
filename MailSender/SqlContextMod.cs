using MailSender.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailSender;

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
    public DbSet<EmailListFile> EmailListFile { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("mailsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("Default");
        optionsBuilder.UseSqlServer(connectionString);
    }
}
