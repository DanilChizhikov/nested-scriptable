# Nested Scriptable
![](https://img.shields.io/badge/unity-2022.3+-000.svg)

## Description
This package allows you to create nested ScriptableObjects lists inside other ScriptableObjects.


## Table of Contents
- [Getting Started](#Getting-Started)
    - [Install manually (using .unitypackage)](#Install-manually-(using-.unitypackage))
    - [Install via UPM (using Git URL)](#Install-via-UPM-(using-Git-URL))
- [How to using](#How-to-using)
- [License](#License)

## Getting Started
Prerequisites:
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 2022.3+

### Install manually (using .unitypackage)
1. Download the .unitypackage from [releases](https://github.com/DanilChizhikov/nested-scriptable/releases/) page.
2. Open NestedScriptable.x.x.x.unitypackage

### Install via UPM (using Git URL)
1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies": { line
    - ```json title="Packages/manifest.json"
      "com.danilchizhikov.nestedso": "https://github.com/DanilChizhikov/nested-scriptable.git?path=Assets/Scriptable",
      ```
UPM should now install the package.

### How to using
In order to use it, it is enough to perform the following actions:
```csharp
public class ExampleScriptableCollection : ScriptableObject
{
    [SerializeField, NestedScriptable] private ExampleScriptable[] _infosArr;
    [SerializeField, NestedScriptable] private List<ExampleScriptable> _infosList;
}

public class ExampleScriptable : ScriptableObject { }
```

Very important, nested scriptable only work with ScriptableObjects!

## License

MIT