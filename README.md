# PK.Swagger.Extension
Swagger接口导出到MarkDown文档

使用方法：

```C#
GlobalConfiguration.Configuration
.EnableSwaggerExport()
.EnableSwaggerUi(c =>
                    {
                        c.InjectExportScript();
                    });
```                    
