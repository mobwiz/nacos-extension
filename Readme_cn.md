# nacos-sdk-csharp Extension

Nacos 扩展，用于支持配置间的应用



例如有一个基础配置如下

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

有另外一个配置需要引用上述配置值
```json
{
    "ConnectionStrings": {
        "DefaultConnection": "server=${Database.Host};port=${Database.Port};user id=${Database.User};password=${Database.Password};database=somedb;CharSet=utf8mb4;SslMode=none"
    }
}
```

使用该插件可实现上述引用机制。

### 实现机制
* 基于 nacos sdk IConfigFilter 实现

### 规范
* 被引用的配置，必须先加载，只能向前引用。在 NacosConfig 配置中的体现是，备用的 dataId，一定要在 Listener 配置中放在最前端。
* 引用语法：  ${keylvl1.keylvl2}
    * 使用 ${} 包含 key
    * 多级key之间通过 . 隔开
    * ***所引用的 key 无法找到时，会直接退出进程，无法启动***




## 安装

通过 nuget 包安装

```shell
```

## 用法

### Configuration

修改 appsettings.json 里的 NacosConfig

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
将 "Nacos.ConfigFilter.Reference" 加到 "ConfigFilterAssemblies" 即可加载该插件。


***注意*** 需要应用的 DataId，在Listener中配置要注意顺序，被引用的要放在最前面。


### 代码修改

```c#

builder.WebHost.ConfigureAppConfiguration((context, builder) =>
    {
        var c = builder.Build();

        // 设置解析器，与下面加载的配置一致
        NacosReferenceConfigHandler.SetParser(Nacos.YamlParser.YamlConfigurationStringParser.Instance);
        // 设置 Group 白名单，不设置则将所有配置视为可引用的基础配置
        NacosReferenceConfigHandler.SetBaseGroupList("DEFAULT_GROUP","COMMON_GROUP" );
        // 设置 DataId 白名单，不涉及则将所有配置视为可引用的基础配置
        NacosReferenceConfigHandler.SetBaseDataIdList("middleware");
        // 此处先检查 Group，再检查 DataId

        // 配置 nacos
        builder.AddNacosV2Configuration(c.GetSection("NacosConfig"),
                        parser: Nacos.YamlParser.YamlConfigurationStringParser.Instance,
                        logAction: null);
                });

```

### Contact
If you have any questions, you can leave an issue here.






