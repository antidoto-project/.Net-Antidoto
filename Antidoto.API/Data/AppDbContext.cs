using Antidoto.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Antidoto.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<ChaveIdempotencia> ChavesIdempotencia => Set<ChaveIdempotencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Prefixo TB_ANT_ para não colidir com as tabelas do Afetto no mesmo usuário Oracle
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("TB_ANT_USUARIO");
            e.Property(x => x.Nome).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.SenhaHash).HasMaxLength(100);
            e.Property(x => x.Role).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Unidade>(e =>
        {
            e.ToTable("TB_ANT_UNIDADE");
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Endereco).HasMaxLength(300);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.Property(x => x.Soros).HasMaxLength(300);
        });

        modelBuilder.Entity<Ocorrencia>(e =>
        {
            e.ToTable("TB_ANT_OCORRENCIA");
            e.Property(x => x.GrupoAnimal).HasMaxLength(30);
            e.Property(x => x.Urgencia).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasIndex(x => x.UsuarioId);
        });

        modelBuilder.Entity<ChaveIdempotencia>(e =>
        {
            e.ToTable("TB_ANT_CHAVE_IDEMPOTENCIA");
            e.Property(x => x.HashCorpo).HasMaxLength(64);
            e.Property(x => x.EmProcessamento).HasColumnType("NUMBER(1)");
            e.Property(x => x.RespostaJson).HasColumnType("CLOB");
            // Mesma chave do mesmo usuário só existe uma vez, mesmo com requisições simultâneas
            e.HasIndex(x => new { x.Chave, x.UsuarioId }).IsUnique();
        });

        Seed(modelBuilder);
    }

    // ── SEED ──────────────────────────────────────────────────────────────────
    public static readonly Guid GestorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string GestorEmail = "gestor@antidoto.gov.br";
    public const string GestorSenha = "Gestor@2026";
    private const string Telefone = "0800 722 6001"; // Disque-Intoxicação (Anvisa)

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = GestorId,
            Nome = "Gestor da Prefeitura",
            Email = GestorEmail,
            // BCrypt de "Gestor@2026" (fixo para a migration não mudar a cada build)
            SenhaHash = "$2a$11$gcfeGSMp2M1cCAb8q.tO2.8tUTXcKKJxnRxsxvI7a/m2ULuVdqV7S",
            Role = Roles.Gestor,
            CriadoEm = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc)
        });

        // Unidades de exemplo para a demonstração (coordenadas aproximadas)
        modelBuilder.Entity<Unidade>().HasData(
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Nome = "Hospital Vital Brazil (Instituto Butantan)", Endereco = "Av. Vital Brasil, 1500 - Butantã, São Paulo - SP", Telefone = Telefone, Latitude = -23.5672, Longitude = -46.7193, Soros = "BOTROPICO;CROTALICO;LAQUETICO;ELAPIDICO;ESCORPIONICO;ARACNIDICO;LOXOSCELICO;LONOMICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Nome = "Hospital das Clínicas da FMUSP", Endereco = "Av. Dr. Enéas de Carvalho Aguiar, 255 - Cerqueira César, São Paulo - SP", Telefone = Telefone, Latitude = -23.5576, Longitude = -46.6688, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Nome = "Hospital São Paulo (Unifesp)", Endereco = "R. Napoleão de Barros, 715 - Vila Clementino, São Paulo - SP", Telefone = Telefone, Latitude = -23.598, Longitude = -46.643, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Nome = "Hospital de Clínicas da Unicamp", Endereco = "R. Vital Brasil, 251 - Cidade Universitária, Campinas - SP", Telefone = Telefone, Latitude = -22.8268, Longitude = -47.061, Soros = "BOTROPICO;CROTALICO;ELAPIDICO;ESCORPIONICO;ARACNIDICO;LOXOSCELICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Nome = "Hospital das Clínicas de Ribeirão Preto", Endereco = "R. Ten. Catão Roxo, 3900 - Vila Monte Alegre, Ribeirão Preto - SP", Telefone = Telefone, Latitude = -21.165, Longitude = -47.858, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Nome = "Hospital das Clínicas de Botucatu (Unesp)", Endereco = "Av. Prof. Mário Rubens Guimarães Montenegro, s/n - Botucatu - SP", Telefone = Telefone, Latitude = -22.889, Longitude = -48.496, Soros = "BOTROPICO;CROTALICO;ELAPIDICO;ESCORPIONICO;LONOMICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Nome = "Hospital de Base de São José do Rio Preto", Endereco = "Av. Brg. Faria Lima, 5544 - São José do Rio Preto - SP", Telefone = Telefone, Latitude = -20.834, Longitude = -49.4, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000008"), Nome = "Hospital Estadual de Bauru", Endereco = "Av. Eng. Luís Edmundo Carrijo Coube, 1-100 - Bauru - SP", Telefone = Telefone, Latitude = -22.349, Longitude = -49.031, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000009"), Nome = "Hospital Regional de Sorocaba", Endereco = "Rod. Raposo Tavares, km 110 - Sorocaba - SP", Telefone = Telefone, Latitude = -23.485, Longitude = -47.425, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000010"), Nome = "Santa Casa de Santos", Endereco = "Av. Dr. Cláudio Luiz da Costa, 50 - Jabaquara, Santos - SP", Telefone = Telefone, Latitude = -23.945, Longitude = -46.336, Soros = "BOTROPICO;ELAPIDICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000011"), Nome = "Hospital Regional de Registro", Endereco = "Av. Prefeito Jonas Banks Leite, 1000 - Registro - SP", Telefone = Telefone, Latitude = -24.495, Longitude = -47.842, Soros = "BOTROPICO;LAQUETICO;ELAPIDICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000012"), Nome = "Hospital Regional de Presidente Prudente", Endereco = "Av. Cel. José Soares Marcondes, 3758 - Presidente Prudente - SP", Telefone = Telefone, Latitude = -22.115, Longitude = -51.4, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000013"), Nome = "Hospital Regional do Vale do Paraíba", Endereco = "Av. Tiradentes, 280 - Taubaté - SP", Telefone = Telefone, Latitude = -23.027, Longitude = -45.556, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO;LONOMICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000014"), Nome = "Santa Casa de Piracicaba", Endereco = "Av. Independência, 953 - Piracicaba - SP", Telefone = Telefone, Latitude = -22.728, Longitude = -47.644, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO" },
            new Unidade { Id = Guid.Parse("10000000-0000-0000-0000-000000000015"), Nome = "Hospital das Clínicas de Marília", Endereco = "R. Aziz Atallah, s/n - Marília - SP", Telefone = Telefone, Latitude = -22.228, Longitude = -49.96, Soros = "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO" }
        );
    }
}
