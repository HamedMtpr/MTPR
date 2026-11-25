# M5 Helpdesk Sample

This directory contains a realistic helpdesk application built entirely from configuration files. It demonstrates:

- **Customer** entity with unique email constraint
- **Ticket** entity with:
  - Reference to customer (restrict delete)
  - Default values for priority, hours, and hourlyRate
  - Computed cost field (hours × hourlyRate)
  - Lifecycle states: open → inProgress → resolved → closed
  - Business rule: reject tickets estimated above 40 hours
- **TicketComment** entity with cascade delete when ticket is removed

This sample exercises all core M0-M5 features: entities, fields, relations, unique constraints, defaults, computed values, lifecycle transitions, and rejection rules.

## Files

- `customer.json` - Customer entity with name and unique email
- `ticket.json` - Ticket entity with lifecycle, computed cost, and estimation rule
- `ticketcomment.json` - Comment entity with cascade delete

## Usage

Point `Mtpr:DefinitionsPath` at this directory.

- [M5 alpha release: build and run](../../building/m5_alpha_release_build.md)
