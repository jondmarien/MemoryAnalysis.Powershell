from volatility3 import framework
from volatility3.framework import contexts, plugins, automagic
from volatility3.plugins.windows import pslist

dump_path = r'F:\physmem.raw'
file_url = 'file:///' + dump_path.replace('\\', '/')
print(f"Dump path: {dump_path}")
print(f"File URL: {file_url}")

# Create context
ctx = contexts.Context()
ctx.config['automagic.LayerStacker.single_location'] = file_url

# Setup plugin
plugin_class = pslist.PsList
base_config_path = 'plugins'

# Get available automagics
automagics = automagic.available(ctx)

# Choose automagics for the plugin
plugin_list = automagic.choose_automagic(automagics, plugin_class)

# Run automagics
print("Running automagics...")
errors = automagic.run(plugin_list, ctx, plugin_class, base_config_path, None)
if errors:
    print(f"Automagic errors: {errors}")
    for err in errors:
        print(f"  - {err}")
else:
    print("Automagics ran successfully!")
    
# Try to construct and run plugin
try:
    plugin = plugins.construct_plugin(ctx, plugin_list, plugin_class, base_config_path, None, None)
    treegrid = plugin.run()
    print("Plugin ran successfully!")
    
    # Count results
    results = []
    def visitor(node, accumulator):
        if accumulator is not None:
            accumulator.append(node)
        return accumulator
    
    treegrid.populate(visitor, results)
    print(f"Found {len(results)} processes")
    
except Exception as e:
    print(f"Error: {e}")
    import traceback
    traceback.print_exc()
