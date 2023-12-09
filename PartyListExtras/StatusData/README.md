# Format

Each JSON file is a list of status effects parsed as `StatusEffectData` structs.
See `StatusEffectData.cs` for actual parsed data.

```
"row_id": [int, id in excel data],
"status_name": [str, human readable name],
"target_type": [enum],
"cond": [
    {
        "<variable>": <value>,
        "then": {
            <AppliedEffects>
        }
    }
],
"cond_default": {
    <AppliedEffects>
}
"cond_else": {
    <AppliedEffects>
}
```

`cond` is a list of conditions.
The effects under the `this` key will be applied if ALL condtions are met.

`cond_else` is appled if all `cond`s are not met.

`cond_default` is always applied even if either of the above are.

## `<AppliedEffects>`

These blocks are parsed as `StatusEffectData.AppliedEffect` objects.

`special` is a list of strings belonging to `Utils.BoolEffect`.
All other keys are floats belonging to `Utils.FloatEffect`.

All of the above is optional.
