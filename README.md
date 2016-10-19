# prim-exporter
Libraries that convert primitive objects to other formats

Expects a halcyon checkout and build in the same parent directory to resolve dependencies

eg:

halcyon/

prim-exporter/

```
Usage:
  -u, --userid=VALUE         Specifies the user who's inventory to load items
                               from
  -i, --invitemid=VALUE      Specifies the inventory ID of the item to load
  -f, --formatter=VALUE      Specifies the object formatter to use
                               (ThreeJSONFormatter, BabylonJSONFormatter)
  -l, --primlimit=VALUE      Specifies a limit to the number of prims in a
                               group
  -c, --checks=VALUE         Specifies the permissions checks to run
  -o, --output=VALUE         Specifies the output directory
  -d, --direct               Dump the generated files directly in the output
                               dir
  -p, --packager=VALUE       Specifies the packager (ThreeJSONPackager,
                               BabylonJSONPackager)
  -s, --stream               Stream XML input from STDIN
  -x, --xmlfile=VALUE        Open the given XML file for input
  -?, --help                 Prints this help message
```
