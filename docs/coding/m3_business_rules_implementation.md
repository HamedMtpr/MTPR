# M3 - Business rules: implementation notes

[Back to index](index.md)

## Literal field defaults

A field definition may include a literal `default`, for example:

```json
{ "name": "priority", "type": "int", "min": 0, "default": 2 }
```

On create, the default is used only when the field is absent (field names are matched case-insensitively). An explicit `null` is not replaced; a required field still rejects it. The definition verifier checks the default against the field type and constraints. Defaults do not apply to PUT or PATCH, and changing a default does not rewrite existing records. Editing the definition changes subsequent creates without rebuilding.

## Internal rule interpreter

Entity definitions may declare up to 32 rules using `beforeCreate`, `afterCreate`, `beforeUpdate`, `afterUpdate`, `beforeDelete`, or `afterDelete`, and action `reject`. Rules for each trigger run in declaration order. A matching rule rejects the request with HTTP 422 and code `MTPR-2011`. Definitions reload when their files change.

```json
"rules": [{
  "trigger": "beforeCreate",
  "condition": {
    "operator": "greaterThan",
    "left": { "field": "priority" },
    "right": { "value": 5 }
  },
  "action": { "type": "reject",
  "message": "Priority is too high" }
}]
```

Comparison operators are `equal`, `notEqual`, `greaterThan`, `greaterThanOrEqual`, `lessThan` and `lessThanOrEqual`. The right side is a literal matching the referenced field type. Ordering comparisons support numeric fields only; comparisons with a null field are false except equality and inequality. `and` and `or` contain a `conditions` array with 1 to 32 conditions. Each rule permits at most 128 condition nodes and depth 12. Invalid or unsupported rules reject the definition. Conditions do not invoke methods or access external resources. Field names are matched case-insensitively; strings are compared ordinally and case-sensitively. JSON Schema validates structure while runtime verification checks field references, types and limits.

Only rejection actions are implemented; do not use rule definitions as authorization. The schema draft under `$defs.rule` is unrelated to the supported entity `rules` property.

## Computed numeric fields

A decimal field can declare a `computed` expression:

```json
{ 
   "name": "total",
   "type": "decimal", 
   "required": true,
   "computed": 
  { 
     "operator": "multiply",
     "left": { "field": "price" },
     "right": { "field": "quantity" }
  } 
}
```

Expressions support nested `add`, `subtract`, and `multiply` with field and decimal literal nodes. Source fields must be ordinary `int`, `long`, or `decimal` fields on the same entity; computed fields cannot depend on computed fields. An expression is limited to 64 nodes and depth 8. Computed fields cannot specify a default or reference. Clients cannot supply them in POST, PATCH, or PUT, including explicit null. A missing or null operand produces null; a required computed field rejects null. Decimal overflow and output constraints cause a validation failure.

Computed values are calculated on create after defaults and input validation and before rules. On update they are recalculated from the merged record inside the same transaction, before lifecycle checks and rules, and stored together with the other changes. Reads, sorting and filtering use the stored value. Changing an expression or adding an optional computed field does not backfill existing records; each record is recomputed on its next successful update, so reads may contain earlier results or null until then.
## Lifecycle state machine

An entity may define one lifecycle on a required string field:

```json
"lifecycle": {
  "field": "status",
  "initial": "draft",
  "states": ["draft", "review", "done"],
  "transitions": [
    { "from": "draft", "to": "review" },
    { "from": "review", "to": "done" }
  ]
}
```

On create an omitted state receives `initial`; a supplied state must equal it. A field `default`, if present, must equal `initial`. States are case-sensitive and must satisfy the field constraints. PATCH and PUT may preserve the current state; a different state must follow a listed directed transition. Create and update reject invalid states or transitions with HTTP 422 and code `MTPR-2012`. Lifecycle validation runs before the corresponding before rule; an after rule can still reject and roll back the operation. An update reads the existing state and checks the transition inside the same immediate SQLite transaction.

Definitions may be reloaded without rebuilding. If a definition removes a state already stored in a record, updates to that record are rejected until its state is valid in the active definition; lifecycle changes do not migrate existing data. Cascaded database changes do not run lifecycle validation on related records.
## Expression engine decision

The M0 recommendation to reuse an existing expression engine is superseded for this initial bounded subset: the MTPR project selected an internal interpreter after package licensing and Windows Application Control issues. This avoids a new runtime package but makes MTPR responsible for the language semantics, limits and tests. Expression Trees remain a possible execution backend later; the JSON format does not expose .NET code.
## Transaction boundary

Create, update and delete use an immediate SQLite transaction. The before trigger runs before the write; after runs after the write but before commit. Rejection at either point rolls the operation back, including SQLite cascades. Update conditions see the proposed complete record (existing values merged with the request); delete conditions see the record being deleted. After-create and after-update conditions see the persisted values, while after-delete sees the pre-delete record. The record id is not available in condition expressions. Update responses are read inside the transaction.

Rules run for the directly requested entity only. Cascading deletes and set-null changes performed by SQLite do not execute rules for affected related records. Actions must remain free of external side effects. Integration tests exercise concurrent updates and computed value recalculation.