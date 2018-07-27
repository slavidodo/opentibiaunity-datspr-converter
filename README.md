# opentibiaunity-datspr-converter

### Installing Packages
I've noticed that dlls are not uploaded (gitignore), consider getting them manually.

https://www.nuget.org/packages/Google.Protobuf  
https://www.nuget.org/packages/Newtonsoft.Json/

```cmd
Install-Package Google.Protobuf -Version 3.6.0
Install-Package Newtonsoft.Json -Version 11.0.2
```

### How to use?

Put tibia.dat, tibia.spr alongside within the application and start it
Once it finishes, appearancesXXX.dat, catalog-content.json and sprites folder will have been generated.
