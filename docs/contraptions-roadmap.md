# Contraption implementation tasks

SchematicBuilderMC creates Minecraft schematic files; these tasks add reusable builder recipes that generate contraption schematics. They do not simulate redstone behavior. Each task should expose a fluent builder API (or a small recipe helper consistent with the existing API), document dimensions/orientation/inputs, and include tests that verify blocks, block states, and any block-entity data.

## 1. Compact Piston Door ([#5](https://github.com/montague247/SchematicBuilderMC/issues/5))

**Task:** Add a configurable 2×2 or 3×3 piston door schematic recipe.

- Generate the door frame, movable door blocks, sticky pistons, and redstone components for the selected size.
- Support configurable orientation and a clear input/trigger position.
- Validate that requested dimensions fit the recipe and do not overwrite unrelated schematic contents unexpectedly.
- Add tests for both sizes and each supported orientation, asserting piston facing, block placement, and schematic bounds.
- Add a README example that writes the result as `.schem`.

## 2. Bubble-Column Elevator ([#6](https://github.com/montague247/SchematicBuilderMC/issues/6))

**Task:** Add a two-way bubble-column elevator schematic recipe.

- Generate a configurable-height shaft with water-source column, soul sand for upward travel, and magma block for downward travel.
- Include entrance/exit openings and allow the shaft to be oriented along either horizontal axis.
- Document any water-source placement assumptions and required player setup.
- Test minimum/typical heights, orientations, and the resulting block states and bounds.

## 3. Item-Sorting Module ([#7](https://github.com/montague247/SchematicBuilderMC/issues/7))

**Task:** Add a single-item filter module, with an option to compose multiple modules into a row.

- Generate the hopper, chest, comparator, redstone torch, and dust/repeater wiring for a filter.
- Accept the filter item and inventory item stacks as inputs; validate slot ranges and counts.
- Store the chest inventory through the existing block-entity/item-stack APIs.
- Test block positions/facing, comparator and redstone states, filter inventory NBT, and multi-module spacing.

## 4. Automatic Smelter ([#8](https://github.com/montague247/SchematicBuilderMC/issues/8))

**Task:** Add a configurable furnace, blast-furnace, or smoker feed-and-fuel schematic.

- Generate the processing block, input/output/fuel chests, and connecting hoppers in the correct directions.
- Allow the machine type and facing to be selected.
- Populate optional chest inventories using existing container helpers without imposing a particular recipe.
- Test all machine types, orientations, hopper directions, inventory serialization, and schematic bounds.

## 5. Compact Crop Farm ([#9](https://github.com/montague247/SchematicBuilderMC/issues/9))

**Task:** Add a crop-farm schematic recipe for a selected supported crop.

- Generate the farmland, water, planting area, collection path, and a collection chest.
- For crops with supported redstone harvesting, include the appropriate observer/piston or dispenser components; document limitations for crops that require manual replanting.
- Allow a configurable planting-row length and orientation.
- Test crop-specific blocks/states, dimensions, and collection block placement.

## 6. Flying Machine ([#10](https://github.com/montague247/SchematicBuilderMC/issues/10))

**Task:** Add a compact, directional flying-machine schematic recipe.

- Generate slime/honey blocks, observers, and pistons with valid facing and placement for the selected travel direction.
- Provide separate starter and end-stop configurations, or document the required external launch/stop components.
- Validate supported directions and reject unsupported configurations with a clear error.
- Test the component layout, observer/piston states, and bounds for every supported direction.

## Implementation notes

- Keep each contraption independently composable and avoid coupling the core schematic model to Minecraft runtime simulation.
- Follow existing naming, fluent API, registry-constant, and test conventions in `src/` and `tests/`.
- Include a generated schematic round-trip test where practical, so block states and block-entity inventories survive serialization.
