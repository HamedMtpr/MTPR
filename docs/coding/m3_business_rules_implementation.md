# M3 - Business rules: implementation notes

[Back to index](index.md)

## Literal field defaults

A field definition may include a literal `default`, for example:

```json
{ "name": "priority", "type": "int", "min": 0, "default": 2 }
```

On create, the default is used only when the field is absent (field names are matched case-insensitively). An explicit `null` is not replaced; a required field still rejects it. The definition verifier checks the default against the field type and constraints. Defaults do not apply to PUT or PATCH, and changing a default does not rewrite existing records. Editing the definition changes subsequent creates without rebuilding.

## Internal rule interpreter

Entity definitions may declare up to 32 rules with trigger `beforeCreate` and action `reject`. Rules run in declaration order after defaults and field validation, before insertion. The first matching rule rejects the request with HTTP 422 and code `MTPR-2011`; no record is inserted. Definitions reload when their files change.

```json
"rules": [{
  "trigger": "beforeCreate",
  "condition": {
    "operator": "greaterThan",
    "left": { "field": "priority" },
    "right": { "value": 5 }
  },
  "action": { "type": "reject", "message": "Priority is too high" }
}]
```

Comparison operators are `equal`, `notEqual`, `greaterThan`, `greaterThanOrEqual`, `lessThan` and `lessThanOrEqual`. The right side is a literal matching the referenced field type. Ordering comparisons support numeric fields only; comparisons with a null field are false except equality and inequality. `and` and `or` contain a `conditions` array with 1 to 32 conditions. Each rule permits at most 128 condition nodes and depth 12. Invalid or unsupported rules reject the definition. Conditions do not invoke methods or access external resources. Field names are matched case-insensitively; strings are compared ordinally and case-sensitively. JSON Schema validates structure while runtime verification checks field references, types and limits.

Only create rejection rules are implemented. Update/delete and after triggers, computed values, and lifecycle transitions remain future M3 work; do not use rule definitions as authorization. The schema draft under `$defs.rule` is unrelated to the supported entity `rules` property.

## Expression engine decision

The M0 recommendation to reuse an existing expression engine is superseded for this initial bounded subset: the MTPR project selected an internal interpreter after package licensing and Windows Application Control issues. This avoids a new runtime package but makes MTPR responsible for the language semantics, limits and tests. Expression Trees remain a possible execution backend later; the JSON format does not expose .NET code.
## Transaction boundary to resolve

Before and after rules, computed values, and state transitions must be coordinated with persistence. In particular, a read followed by an update on separate connections cannot safely enforce a state transition under concurrent requests. Define and test transaction and rollback semantics before enabling update/delete transitions or actions with side effects.


