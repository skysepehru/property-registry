# Property Registry

A reactive, dependency-graph-driven property/stat system for Unity.

You declare properties and the calculators that relate them; the registry builds a
dependency graph, recomputes only the parts affected by a change, and exposes every
value as an [R3](https://github.com/Cysharp/R3) reactive property you can subscribe to.

## Requirements

- Unity **2022.3** or newer
- [**R3**](https://github.com/Cysharp/R3) (`com.cysharp.r3`) — a hard dependency

## Installation

Distributed by git URL. **Install R3 first** (it is not pulled in automatically):

1. Install R3 — via OpenUPM (`com.cysharp.r3`) or its git URL.
2. In Unity, **Package Manager → Add package from git URL**, or add to
   `Packages/manifest.json`:

   ```json
   "com.skysepehru.property-registry": "https://github.com/skysepehru/property-registry.git#v0.1.0"
   ```

Pin a tag (`#v0.1.0`) for reproducible installs; omit it to track the latest commit.

## Concepts

### Global vs per-instance properties

Every property belongs to an **entity**, and each entity is either *global* or has
many *instances*.

- **Global properties** exist exactly once for the whole system — e.g. a
  `GlobalDamageMultiplier` shared by everything. They live on a special **global
  entity** that you designate.
- **Per-instance properties** exist once per instance of an entity — e.g. `Strength`
  for `Character[0]`, `Character[1]`, … Each instance is addressed by an integer
  **instance index**.

The same property *name* can therefore refer to one global value or to a whole set of
per-instance values, depending on which entity it is registered under.

### Base vs derived properties

- **Base properties** are raw inputs. You set them directly with
  `SetBasePropertyValue(...)`. Example: `Strength`, `GlobalDamageMultiplier`.
- **Derived properties** are *computed* from other properties by a **calculator**.
  You never set them directly — the registry produces them. Example:
  `AttackPower = Strength * GlobalDamageMultiplier`.

When a base property changes and you call `Tick()`, every derived property that
(transitively) depends on it is recomputed; nothing else is touched.

## Usage

### 1. Define your enums

Entities and property names are `enum`s **backed by `int`** (the default). The entity
enum **must contain a global member** — you will hand it to the system as the global
entity.

```csharp
public enum Entity { Global, Character }          // Global is the global entity
public enum Stat   { Strength, GlobalDamageMultiplier, AttackPower }
```

### 2. Subclass `PropertySystem`

Your system is a subclass of `PropertySystem<TEntity, TProperty>` that names your two
enums and passes the global entity to the base constructor.

```csharp
using skysepehru.Core.PropertyRegistry;

public sealed class CharacterStats : PropertySystem<Entity, Stat>
{
    public CharacterStats() : base(Entity.Global) { }
}
```

### 3. Write a calculator

A calculator declares its **inputs**, its **output**, and how to compute the output.
It implements `IPropertyCalculator`:

```csharp
using System;
using System.Collections.Generic;
using skysepehru.Core.PropertyRegistry;

// AttackPower[c] = Strength[c] * GlobalDamageMultiplier
public sealed class AttackPowerCalculator : IPropertyCalculator
{
    // Declare inputs by writing filters into the buffer, in order.
    // Return how many you wrote.
    public int GetInputProperties(Span<PropertyFilter> buffer)
    {
        buffer[0] = PropertyFilter.New(Stat.Strength, Entity.Character);              // input 0
        buffer[1] = PropertyFilter.New(Stat.GlobalDamageMultiplier, Entity.Global);   // input 1
        return 2;
    }

    public PropertyFilter GetOutputProperty()
        => PropertyFilter.New(Stat.AttackPower, Entity.Character);

    public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs,
                          List<Property> outputs)
    {
        // inputs[0] -> Strength    (per-instance: one entry per Character)
        // inputs[1] -> multiplier  (global: a single entry at [0])
        var strengths  = inputs[0];
        var multiplier = inputs[1][0].ReadOnlyValueReactive.CurrentValue;

        for (int i = 0; i < outputs.Count; i++)
        {
            if (!outputs[i].IsDirtied) continue; // skip instances that didn't change
            outputs[i].ValueReactive.Value =
                strengths[i].ReadOnlyValueReactive.CurrentValue * multiplier;
        }
    }
}
```

> **The input/output API is positional — mind the indices.** There are no names in
> `Calculate`. `inputs` arrives in the **exact order** you wrote filters in
> `GetInputProperties` (`inputs[0]` is your first filter, `inputs[1]` the second, …),
> and each entry is itself a list over that property's instances — a single element
> `[0]` for global properties, one element per instance for per-instance properties.
> `outputs` is the instance list for your output property. Keeping these indices in
> sync is your responsibility; if you reorder the filters, update `Calculate` to match.

### 4. Register everything and use it

**Register all properties before the calculators that reference them** — a calculator
is wired against existing property nodes, so they must already be registered.

```csharp
using R3;

var stats = new CharacterStats();

// --- base properties (raw inputs) ---
stats.RegisterBaseGlobalProperty(Stat.GlobalDamageMultiplier, initialValue: 1.5);

// two characters, instance indices 0 and 1
stats.RegisterBaseInstancedProperty(Stat.Strength, Entity.Character, instanceIndex: 0, initialValue: 10);
stats.RegisterBaseInstancedProperty(Stat.Strength, Entity.Character, instanceIndex: 1, initialValue: 20);

// --- derived properties (one per character) ---
stats.RegisterDerivedInstancedProperty(Stat.AttackPower, Entity.Character, instanceIndex: 0);
stats.RegisterDerivedInstancedProperty(Stat.AttackPower, Entity.Character, instanceIndex: 1);

// --- calculators, AFTER the properties they touch ---
stats.RegisterCalculator<AttackPowerCalculator>();

stats.Tick(); // evaluate the dirty graph

// Read reactively — fires now with the current value, and again on every change.
stats[Stat.AttackPower, Entity.Character, 0].Subscribe(v => Debug.Log($"Char0 AP = {v}")); // 15
stats[Stat.AttackPower, Entity.Character, 1].Subscribe(v => Debug.Log($"Char1 AP = {v}")); // 30

// Change a base input and re-evaluate:
stats.SetBasePropertyValue(Stat.Strength, Entity.Character, value: 25, instanceIndex: 0);
stats.Tick(); // Char0 AP subscriber fires again: 37.5
```

## Key points

- **Register properties before calculators.**
- **Base** properties are set with `SetBasePropertyValue`; **derived** properties are
  produced by calculators and cannot be set directly.
- Use the `...Global...` overloads for the global entity and the `...Instanced...`
  overloads for per-instance entities (the global entity is rejected there).
- Call **`Tick()`** after changing inputs or registering calculators.
- **Cycles are rejected** at calculator registration time.

## License

[MIT](LICENSE.md)
