using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities.Configs
{
    class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
    {
        public void Configure(EntityTypeBuilder<Episode> builder)
        {
            builder.ToTable("T_Episodes");
            builder.HasOne(e => e.Album).WithMany().HasForeignKey(e => e.AlbumId).IsRequired();
        }
    }
}
