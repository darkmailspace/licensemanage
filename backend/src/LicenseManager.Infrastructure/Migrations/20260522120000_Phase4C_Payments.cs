using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicenseManager.Infrastructure.Migrations;

/// <summary>
/// Phase 4C — adds the three tables that back the Razorpay + Stripe payment
/// subsystem (payments, refunds, webhook_events). Schema is identical to the
/// hand-authored database/migrations/005_payments.sql; either path applies,
/// never both.
///
/// The three new tables have FKs into license_customers (Restrict) and
/// licenses (SetNull). Those parent tables are created by the SQL migration
/// 001_initial_schema.sql and are intentionally NOT modeled in this EF
/// migration's snapshot — see ApplicationDbContextModelSnapshot.cs for why.
/// </summary>
public partial class Phase4C_Payments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // -----------------------------------------------------------------
        // payments
        // -----------------------------------------------------------------
        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                id                       = table.Column<Guid>(type: "uuid",                        nullable: false),

                customer_id              = table.Column<Guid>(type: "uuid",                        nullable: false),
                license_id               = table.Column<Guid>(type: "uuid",                        nullable: true),

                provider                 = table.Column<int>(type: "integer",                       nullable: false),
                provider_payment_id      = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: true),
                provider_order_id        = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: true),
                provider_customer_id     = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: true),

                amount                   = table.Column<decimal>(type: "numeric(18,2)",             precision: 18, scale: 2, nullable: false),
                amount_minor             = table.Column<long>(type: "bigint",                        nullable: false),
                currency                 = table.Column<string>(type: "character varying(3)",       maxLength: 3,    nullable: false),

                status                   = table.Column<int>(type: "integer",                       nullable: false),
                authorized_at            = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                captured_at              = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                failed_at                = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                cancelled_at             = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                refunded_amount_minor    = table.Column<long>(type: "bigint",                        nullable: false, defaultValue: 0L),

                client_secret            = table.Column<string>(type: "character varying(512)",     maxLength: 512,  nullable: true),
                checkout_url             = table.Column<string>(type: "character varying(2048)",    maxLength: 2048, nullable: true),
                receipt                  = table.Column<string>(type: "character varying(64)",      maxLength: 64,   nullable: true),
                description              = table.Column<string>(type: "character varying(512)",     maxLength: 512,  nullable: true),

                error_code               = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: true),
                error_message            = table.Column<string>(type: "character varying(2048)",    maxLength: 2048, nullable: true),
                raw_provider_data        = table.Column<string>(type: "text",                        nullable: true),
                metadata                 = table.Column<string>(type: "jsonb",                       nullable: true),

                // BaseEntity
                created_at               = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at               = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                created_by               = table.Column<string>(type: "text",                        nullable: true),
                updated_by               = table.Column<string>(type: "text",                        nullable: true),
                is_deleted               = table.Column<bool>(type: "boolean",                       nullable: false, defaultValue: false),
                deleted_at               = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_payments", x => x.id);

                table.ForeignKey(
                    name:             "fk_payments_customer",
                    column:           x => x.customer_id,
                    principalTable:   "license_customers",
                    principalColumn:  "id",
                    onDelete:         ReferentialAction.Restrict);

                table.ForeignKey(
                    name:             "fk_payments_license",
                    column:           x => x.license_id,
                    principalTable:   "licenses",
                    principalColumn:  "id",
                    onDelete:         ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("ix_payments_provider_provider_payment_id", "payments", new[] { "provider", "provider_payment_id" });
        migrationBuilder.CreateIndex("ix_payments_provider_provider_order_id",   "payments", new[] { "provider", "provider_order_id"   });
        migrationBuilder.CreateIndex("ix_payments_customer_id",                   "payments", "customer_id");
        migrationBuilder.CreateIndex("ix_payments_license_id",                    "payments", "license_id");
        migrationBuilder.CreateIndex("ix_payments_status",                        "payments", "status");
        migrationBuilder.CreateIndex("ix_payments_created_at",                    "payments", "created_at");

        // -----------------------------------------------------------------
        // refunds
        // -----------------------------------------------------------------
        migrationBuilder.CreateTable(
            name: "refunds",
            columns: table => new
            {
                id                 = table.Column<Guid>(type: "uuid",                        nullable: false),

                payment_id         = table.Column<Guid>(type: "uuid",                        nullable: false),
                provider_refund_id = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: true),

                amount             = table.Column<decimal>(type: "numeric(18,2)",             precision: 18, scale: 2, nullable: false),
                amount_minor       = table.Column<long>(type: "bigint",                        nullable: false),
                currency           = table.Column<string>(type: "character varying(3)",       maxLength: 3,    nullable: false),

                status             = table.Column<int>(type: "integer",                       nullable: false),
                reason             = table.Column<string>(type: "character varying(256)",     maxLength: 256,  nullable: true),
                error_message      = table.Column<string>(type: "character varying(2048)",    maxLength: 2048, nullable: true),
                refunded_at        = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                raw_provider_data  = table.Column<string>(type: "text",                        nullable: true),

                // BaseEntity
                created_at         = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at         = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                created_by         = table.Column<string>(type: "text",                        nullable: true),
                updated_by         = table.Column<string>(type: "text",                        nullable: true),
                is_deleted         = table.Column<bool>(type: "boolean",                       nullable: false, defaultValue: false),
                deleted_at         = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refunds", x => x.id);

                table.ForeignKey(
                    name:             "fk_refunds_payment",
                    column:           x => x.payment_id,
                    principalTable:   "payments",
                    principalColumn:  "id",
                    onDelete:         ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_refunds_payment_id",          "refunds", "payment_id");
        migrationBuilder.CreateIndex("ix_refunds_provider_refund_id",  "refunds", "provider_refund_id");
        migrationBuilder.CreateIndex("ix_refunds_status",              "refunds", "status");

        // -----------------------------------------------------------------
        // webhook_events
        // -----------------------------------------------------------------
        migrationBuilder.CreateTable(
            name: "webhook_events",
            columns: table => new
            {
                id                = table.Column<Guid>(type: "uuid",                        nullable: false),

                provider          = table.Column<int>(type: "integer",                       nullable: false),
                provider_event_id = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: false),
                event_type        = table.Column<string>(type: "character varying(128)",     maxLength: 128,  nullable: false),

                status            = table.Column<int>(type: "integer",                       nullable: false),
                payload           = table.Column<string>(type: "text",                        nullable: false),
                signature         = table.Column<string>(type: "character varying(2048)",    maxLength: 2048, nullable: true),
                received_at       = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                processed_at      = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                error_message     = table.Column<string>(type: "character varying(2048)",    maxLength: 2048, nullable: true),
                payment_id        = table.Column<Guid>(type: "uuid",                        nullable: true),

                // BaseEntity
                created_at        = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at        = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                created_by        = table.Column<string>(type: "text",                        nullable: true),
                updated_by        = table.Column<string>(type: "text",                        nullable: true),
                is_deleted        = table.Column<bool>(type: "boolean",                       nullable: false, defaultValue: false),
                deleted_at        = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_webhook_events", x => x.id);
            });

        // The UNIQUE on (provider, provider_event_id) is the actual enforcement
        // of webhook idempotency. The application-level dedup check is just an
        // optimisation; this index is the source of truth.
        migrationBuilder.CreateIndex(
            name:    "ix_webhook_events_provider_provider_event_id",
            table:   "webhook_events",
            columns: new[] { "provider", "provider_event_id" },
            unique:  true);

        migrationBuilder.CreateIndex("ix_webhook_events_status",      "webhook_events", "status");
        migrationBuilder.CreateIndex("ix_webhook_events_received_at", "webhook_events", "received_at");
        migrationBuilder.CreateIndex("ix_webhook_events_payment_id",  "webhook_events", "payment_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop in reverse dependency order: webhook_events has no FKs out,
        // refunds depends on payments, payments depends on the existing
        // license_customers and licenses tables.
        migrationBuilder.DropTable("webhook_events");
        migrationBuilder.DropTable("refunds");
        migrationBuilder.DropTable("payments");
    }
}
