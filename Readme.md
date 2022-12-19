# nacos-sdk-csharp Extension

An extension for charp(dotnet core) to use nacos for configuration refrence just like below:

There is a base config
```json
{
    "Database" {
        "Host": "192.168.0.100",
        "Port": 3306,
        "User": "dbuser",
        "Password": "dbpass"
    }
}
```
Another configuration reference to this config like this:
```json
{
    "ConnectionStrings": {
        "DefaultConnection": "server=${Database.Host};port=${Database.Port};user id=${Database.User};password=${Database.Password};database=somedb;CharSet=utf8mb4;SslMode=none"
    }
}
```



## Installation

Install via nuget

```shell
```

## Usage

### Configuration
Put below in your appsettings.json

```json

{
    "NacosConfig": {
        "Listeners": [
            {...}
        ],
        "Namespace": "your nacos namespace id",
        "ServerAddresses": [ "http://yourNacosServer:8848" ],
        "ConfigFilterAssemblies": [ "Nacos.ConfigFilter.Reference" ],
        "ConfigFilterExtInfo": "{}"
    }
}

```

* The only change to your NacosConfig it to add this assembly name "Nacos.ConfigFilter.Reference" to "ConfigFilterAssemblies"

* ***Attention*** Base config listener must be placed from top to bottom.
* ***Attention*** If a referenced key is not found, the application will exit!


### Code

```c#

builder.WebHost.ConfigureAppConfiguration((context, builder) =>
    {
        var c = builder.Build();

        // set the parser which is default to JsonParser
        NacosReferenceConfigHandler.SetParser(Nacos.YamlParser.YamlConfigurationStringParser.Instance);
        // set the groups can be used as base config can be referenced to .
        NacosReferenceConfigHandler.SetBaseGroupList("DEFAULT_GROUP", "COMMON_GROUP");
        // set the groups can be used as base config can be referenced by other configs.
        NacosReferenceConfigHandler.SetBaseDataIdList("middleware");
        // The process order is group first
        // These two options can be ignored, but if you have a lot of DataIds, it is recommended to set there two options to decrease memory use.


        builder.AddNacosV2Configuration(c.GetSection("NacosConfig"),
                        parser: Nacos.YamlParser.YamlConfigurationStringParser.Instance,
                        logAction: null);
                });

```

### Contact
If you have any questions, you can leave an issue here.






