using Impodatos.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Impodatos.Persistence.Database.Configuration
{
    public class historyConfiguration
    {
        public static void SetEntityBuilder(EntityTypeBuilder<history> entityBuilder)
        {
            entityBuilder.HasIndex(x => x.id);
            entityBuilder.HasIndex(x => x.uploads);
            entityBuilder.HasIndex(x => x.deleted);
            entityBuilder.Property(x => x.programsid).IsRequired().HasMaxLength(100);
            entityBuilder.Property(x => x.jsonset).IsRequired();
            entityBuilder.Property(x => x.state).IsRequired();
            entityBuilder.Property(x => x.userlogin).IsRequired().HasMaxLength(300);
            entityBuilder.Property(x => x.fecha).IsRequired();
            entityBuilder.Property(x => x.file).IsRequired();
            entityBuilder.Property(x => x.country).IsRequired();
            entityBuilder.Property(x => x.namefile).IsRequired();
            entityBuilder.Property(x => x.file1).IsRequired();
            entityBuilder.Property(x => x.namefile1).IsRequired();
        }
    }
}
