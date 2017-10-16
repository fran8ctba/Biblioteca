using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DemoCRUD.Models;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace DemoCRUD.AcessoDados
{
    public class LivroContexto: DbContext
    {
        public DbSet<Genero> Generos { get; set; }
        public DbSet<Livro> Livros { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Properties<string>().Configure(c => c.HasMaxLength(100));
        }
    }
}