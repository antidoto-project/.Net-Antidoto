using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Antidoto.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_ANT_CHAVE_IDEMPOTENCIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Chave = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    HashCorpo = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    EmProcessamento = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    StatusCode = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    RespostaJson = table.Column<string>(type: "CLOB", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ANT_CHAVE_IDEMPOTENCIA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_ANT_UNIDADE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Endereco = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    Telefone = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Soros = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ANT_UNIDADE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_ANT_USUARIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    SenhaHash = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ANT_USUARIO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_ANT_OCORRENCIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Data = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    GrupoAnimal = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Urgencia = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ANT_OCORRENCIA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_ANT_OCORRENCIA_TB_ANT_USUARIO_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "TB_ANT_USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TB_ANT_UNIDADE",
                columns: new[] { "Id", "Endereco", "Latitude", "Longitude", "Nome", "Soros", "Telefone" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Av. Vital Brasil, 1500 - Butantã, São Paulo - SP", -23.5672, -46.719299999999997, "Hospital Vital Brazil (Instituto Butantan)", "BOTROPICO;CROTALICO;LAQUETICO;ELAPIDICO;ESCORPIONICO;ARACNIDICO;LOXOSCELICO;LONOMICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Av. Dr. Enéas de Carvalho Aguiar, 255 - Cerqueira César, São Paulo - SP", -23.557600000000001, -46.668799999999997, "Hospital das Clínicas da FMUSP", "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "R. Napoleão de Barros, 715 - Vila Clementino, São Paulo - SP", -23.597999999999999, -46.643000000000001, "Hospital São Paulo (Unifesp)", "BOTROPICO;CROTALICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "R. Vital Brasil, 251 - Cidade Universitária, Campinas - SP", -22.826799999999999, -47.061, "Hospital de Clínicas da Unicamp", "BOTROPICO;CROTALICO;ELAPIDICO;ESCORPIONICO;ARACNIDICO;LOXOSCELICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "R. Ten. Catão Roxo, 3900 - Vila Monte Alegre, Ribeirão Preto - SP", -21.164999999999999, -47.857999999999997, "Hospital das Clínicas de Ribeirão Preto", "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Av. Prof. Mário Rubens Guimarães Montenegro, s/n - Botucatu - SP", -22.888999999999999, -48.496000000000002, "Hospital das Clínicas de Botucatu (Unesp)", "BOTROPICO;CROTALICO;ELAPIDICO;ESCORPIONICO;LONOMICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Av. Brg. Faria Lima, 5544 - São José do Rio Preto - SP", -20.834, -49.399999999999999, "Hospital de Base de São José do Rio Preto", "BOTROPICO;CROTALICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Av. Eng. Luís Edmundo Carrijo Coube, 1-100 - Bauru - SP", -22.349, -49.030999999999999, "Hospital Estadual de Bauru", "BOTROPICO;CROTALICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Rod. Raposo Tavares, km 110 - Sorocaba - SP", -23.484999999999999, -47.424999999999997, "Hospital Regional de Sorocaba", "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "Av. Dr. Cláudio Luiz da Costa, 50 - Jabaquara, Santos - SP", -23.945, -46.335999999999999, "Santa Casa de Santos", "BOTROPICO;ELAPIDICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Av. Prefeito Jonas Banks Leite, 1000 - Registro - SP", -24.495000000000001, -47.841999999999999, "Hospital Regional de Registro", "BOTROPICO;LAQUETICO;ELAPIDICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "Av. Cel. José Soares Marcondes, 3758 - Presidente Prudente - SP", -22.114999999999998, -51.399999999999999, "Hospital Regional de Presidente Prudente", "BOTROPICO;CROTALICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Av. Tiradentes, 280 - Taubaté - SP", -23.027000000000001, -45.555999999999997, "Hospital Regional do Vale do Paraíba", "BOTROPICO;CROTALICO;ESCORPIONICO;LONOMICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "Av. Independência, 953 - Piracicaba - SP", -22.728000000000002, -47.643999999999998, "Santa Casa de Piracicaba", "BOTROPICO;CROTALICO;ESCORPIONICO", "0800 722 6001" },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "R. Aziz Atallah, s/n - Marília - SP", -22.228000000000002, -49.960000000000001, "Hospital das Clínicas de Marília", "BOTROPICO;CROTALICO;ESCORPIONICO;ARACNIDICO", "0800 722 6001" }
                });

            migrationBuilder.InsertData(
                table: "TB_ANT_USUARIO",
                columns: new[] { "Id", "CriadoEm", "Email", "Nome", "Role", "SenhaHash" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 11, 12, 0, 0, 0, 0, DateTimeKind.Utc), "gestor@antidoto.gov.br", "Gestor da Prefeitura", "gestor", "$2a$11$gcfeGSMp2M1cCAb8q.tO2.8tUTXcKKJxnRxsxvI7a/m2ULuVdqV7S" });

            migrationBuilder.CreateIndex(
                name: "IX_TB_ANT_CHAVE_IDEMPOTENCIA_Chave_UsuarioId",
                table: "TB_ANT_CHAVE_IDEMPOTENCIA",
                columns: new[] { "Chave", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_ANT_OCORRENCIA_UsuarioId",
                table: "TB_ANT_OCORRENCIA",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_ANT_USUARIO_Email",
                table: "TB_ANT_USUARIO",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_ANT_CHAVE_IDEMPOTENCIA");

            migrationBuilder.DropTable(
                name: "TB_ANT_OCORRENCIA");

            migrationBuilder.DropTable(
                name: "TB_ANT_UNIDADE");

            migrationBuilder.DropTable(
                name: "TB_ANT_USUARIO");
        }
    }
}
