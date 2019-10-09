using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities.Configs
{
    class BookConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.ToTable("T_Books");
            //builder.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId).IsRequired();
            builder.HasOne(e => e.Author).WithMany(e=>e.Books).HasForeignKey(e => e.AuthorId).IsRequired();
        }
    }
}
