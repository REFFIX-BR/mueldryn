// <copyright file="20260811210000_AddItemExpirationDate.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

#nullable disable

using MUnique.OpenMU.Persistence.EntityFramework;

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    [DbContext(typeof(EntityDataContext))]
    [Migration("20260811210000_AddItemExpirationDate")]
    public class AddItemExpirationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: column may already exist from a hot-deploy ALTER.
            migrationBuilder.Sql(
                """
                ALTER TABLE data."Item" ADD COLUMN IF NOT EXISTS "ExpirationDate" timestamp with time zone NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE data."Item" DROP COLUMN IF EXISTS "ExpirationDate";
                """);
        }
    }
}
