// <copyright file="20260810190000_AddExcellentBonusStats.cs" company="MUnique">
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
    [Migration("20260810190000_AddExcellentBonusStats")]
    public class AddExcellentBonusStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "ExcellentBonusStrength",
                schema: "data",
                table: "Item",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "ExcellentBonusEnergy",
                schema: "data",
                table: "Item",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcellentBonusStrength",
                schema: "data",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "ExcellentBonusEnergy",
                schema: "data",
                table: "Item");
        }
    }
}
