# OptionalDependAPI

> *"Screw dependencies, shoot a laser on them, make them optional!"*
</blockquote>

## Overview


OptionalDependAPI is a lightweight library, allowing seamless integration with other mods without requiring hard dependencies. This solves the common problem where your mod could benefit from additional features provided by other mods, but you don't want to force users to install those other mods if they don't want or need those extra features.


## The Problem


Ever had a mod that would be great on its own but could have more features if it integrated with other mods? Without a proper solution:

- You either force users to install mods they might not want (hard dependencies)
- Or you miss out on potential integration features altogether


## My Solution


OptionalDependAPI provides a clean, standardized way to handle optional mod dependencies:


1. **Register** your mod's API with OptionalDependAPI
2. **Subscribe** to other mods you want to integrate with
3. **Check** if those mods are available at runtime
4. **Use** their features only when available

No hard dependencies on other mods, no compatibility patches, just simple runtime checks and graceful feature enabling.


### Limitation


One important caveat: mods that want to use this system will need to include OptionalDependAPI as a dependency. 


## Changelog


- **v1.0.0**: Initial release
