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
            builder.ToTable("T_Books").HasKey(b=>b.Id);
            builder.Property(nameof(Book.Id)).HasColumnName("FId").UseSqlServerIdentityColumn();
            builder.Property(nameof(Book.AuthorId)).HasColumnName("FAuthorId");
            builder.Property(nameof(Book.Name)).HasColumnName("FName");
            builder.Property(nameof(Book.Price)).HasColumnName("FPrice");
            builder.Property(nameof(Book.PublishDate)).HasColumnName("FPublishDate");
            builder.HasOne(e => e.Author).WithMany(e=>e.Books).HasForeignKey(e => e.AuthorId).IsRequired();
        }
    }
}
