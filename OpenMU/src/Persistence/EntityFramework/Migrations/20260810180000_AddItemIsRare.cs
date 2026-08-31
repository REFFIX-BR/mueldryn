// <copyright file="20260810180000_AddItemIsRare.cs" company="MUnique">
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
    [Migration("20260810180000_AddItemIsRare")]
    public class AddItemIsRare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRare",
                schema: "data",
                table: "Item",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRare",
                schema: "data",
                table: "Item");
        }
    }
}